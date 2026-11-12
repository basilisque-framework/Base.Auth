using Basilisque.Base.Auth.DataAccess.EntityFramework.Relational;
using Basilisque.Base.Auth.DataAccess.EntityFramework.Relational.Model;
using Basilisque.Base.Auth.Identity;
using Basilisque.Base.Auth.Server.ClientAuth;
using Basilisque.Base.Auth.Server.Extensions;
using Basilisque.Base.Auth.Server.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Basilisque.Base.Auth.MachineService;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.RegisterServices();
        builder.Services.AddBaseAuthRelationalPersistence();
        var issuer = new Uri(builder.Configuration["Auth:Issuer"] ?? throw new InvalidOperationException("Auth:Issuer is required."));
        var audience = builder.Configuration["Auth:Audience"] ?? throw new InvalidOperationException("Auth:Audience is required.");
        builder.Services.AddBaseAuthServer(options =>
        {
            options.Issuer = issuer;
            options.Audience = audience;
        }, options =>
        {
            if (builder.Environment.IsDevelopment())
                options.AddDevelopmentSigningCertificate().AddDevelopmentEncryptionCertificate();
            else
            {
                // Mounted files work on Windows/Linux/Docker. Supply passwords through deployment secrets.
                options.AddSigningCertificate(loadCertificate(builder.Configuration, "Signing"));
                options.AddEncryptionCertificate(loadCertificate(builder.Configuration, "Encryption"));
            }
        });
        builder.Services.AddBaseAuthLocalApi(audience);
        await using var app = builder.Build();
        if (builder.Configuration.GetValue<bool>("initialize-database"))
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
                throw new NotSupportedException("Apply database migrations separately before running this host with Native AOT.");

            await using var scope = app.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.MigrateAsync();
        }
        var operationNames = new[] { "register-client", "replace-client-keys", "disable-client" };
        var selectedOperations = operationNames.Where(name => !string.IsNullOrWhiteSpace(builder.Configuration[name])).ToArray();
        if (selectedOperations.Length > 1) throw new InvalidOperationException("Choose one client administration operation at a time.");
        if (selectedOperations.Length == 1)
        {
            var operation = selectedOperations[0];
            var clientId = builder.Configuration[operation]!;
            await using var scope = app.Services.CreateAsyncScope();
            var registry = scope.ServiceProvider.GetRequiredService<IMachineClientRegistry>();
            if (operation == "disable-client")
                await registry.DisableAsync(clientId);
            else
            {
                var publicKeysPath = builder.Configuration["public-jwks"] ?? throw new InvalidOperationException("--public-jwks is required.");
                var publicKeys = new JsonWebKeySet(await File.ReadAllTextAsync(publicKeysPath));
                if (operation == "replace-client-keys") await registry.ReplacePublicKeysAsync(clientId, publicKeys);
                else
                {
                    var contextId = builder.Configuration["context"] ?? throw new InvalidOperationException("--context is required.");
                    await registry.RegisterAsync(clientId, contextId, publicKeys);
                }
            }
            Console.WriteLine("Client administration completed. No private key was imported.");
            return;
        }
        if (builder.Configuration.GetValue<bool>("prune-assertions"))
        {
            await using var scope = app.Services.CreateAsyncScope();
            var removed = await scope.ServiceProvider.GetRequiredService<AuthDbContext>().PruneExpiredAssertionsAsync();
            Console.WriteLine($"Expired assertion markers removed: {removed}");
            return;
        }
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapBaseAuthTokenEndpoint();
        app.MapGet("/api/ping", (HttpContext context) => TypedResults.Json(context.User.GetBaseAuthActor(), MachineServiceJsonContext.Default.Actor))
            .RequireAuthorization(BaseAuthPolicies.Service);
        await app.RunAsync();
    }

    private static X509Certificate2 loadCertificate(IConfiguration configuration, string purpose)
    {
        var path = configuration[$"Auth:{purpose}CertificatePath"]
            ?? throw new InvalidOperationException($"Auth:{purpose}CertificatePath is required outside Development.");
        return X509CertificateLoader.LoadPkcs12FromFile(path, configuration[$"Auth:{purpose}CertificatePassword"],
            X509KeyStorageFlags.EphemeralKeySet);
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Actor))]
internal sealed partial class MachineServiceJsonContext : JsonSerializerContext
{
}
