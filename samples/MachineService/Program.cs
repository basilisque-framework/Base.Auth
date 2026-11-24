/*
   Copyright 2026 Alexander Stärk

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using Basilisque.Base.Auth.Abstractions.Identity;
using Basilisque.Base.Auth.DataAccess.EntityFramework.Relational;
using Basilisque.Base.Auth.DataAccess.EntityFramework.Relational.Model;
using Basilisque.Base.Auth.Server.ClientAuth;
using Basilisque.Base.Auth.Server.Extensions;
using Basilisque.Base.Auth.Server.Identity;
using Basilisque.DataAccess.EntityFramework.Base.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

namespace Basilisque.Base.Auth.MachineService;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.RegisterServices();
        builder.Services.AddBaseAuthRelationalPersistence();
        var issuer = new Uri(builder.Configuration["Auth:Issuer"] ?? throw new InvalidOperationException(Messages.IssuerRequired));
        var audience = builder.Configuration["Auth:Audience"] ?? throw new InvalidOperationException(Messages.AudienceRequired);
        builder.Services.AddBaseAuthServer(options =>
        {
            options.Issuer = issuer;
            options.Audience = audience;
        }, options =>
        {
            if (builder.Environment.IsDevelopment())
            {
                _ = options.AddDevelopmentSigningCertificate().AddDevelopmentEncryptionCertificate();
            }
            else
            {
                // Mounted files work on Windows/Linux/Docker. Supply passwords through deployment secrets.
                options.AddSigningCertificate(loadCertificate(builder.Configuration, "Signing"));
                options.AddEncryptionCertificate(loadCertificate(builder.Configuration, "Encryption"));
            }
        });
        builder.Services.AddBaseAuthLocalApi(audience);
        builder.Services.AddBaseAuthLocalUsers().AddBaseAuthRelationalUserStore().AddDefaultUI();
        builder.Services.AddAuthorizationBuilder().AddPolicy("LocalAccount", policy =>
            policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme).RequireAuthenticatedUser());
        builder.Services.AddRazorPages(options =>
        {
            options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage", "LocalAccount");
            options.Conventions.AddAreaPageRouteModelConvention("Identity", "/Account/Register",
                model => model.Selectors.Clear());
        });
        await using var app = builder.Build();
        if (builder.Configuration.GetValue<bool>("initialize-database"))
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
                throw new NotSupportedException(Messages.MigrateBeforeNativeAot);

            await using var scope = app.Services.CreateAsyncScope();
            foreach (var initializer in scope.ServiceProvider.GetServices<IDbInitializer>())
                initializer.MigrateDatabase();
        }
        if (await LocalUserAdministration.TryRunAsync(app.Services, builder.Configuration))
            return;
        var operationNames = new[] { "register-client", "replace-client-keys", "disable-client" };
        var selectedOperations = operationNames.Where(name => !string.IsNullOrWhiteSpace(builder.Configuration[name])).ToArray();
        if (selectedOperations.Length > 1)
            throw new InvalidOperationException(Messages.OneClientAdministrationOperation);
        if (selectedOperations.Length == 1)
        {
            var operation = selectedOperations[0];
            var clientId = builder.Configuration[operation]!;
            await using var scope = app.Services.CreateAsyncScope();
            var registry = scope.ServiceProvider.GetRequiredService<IMachineClientRegistry>();
            if (operation == "disable-client")
            {
                await registry.DisableAsync(clientId);
            }
            else
            {
                var publicKeysPath = builder.Configuration["public-jwks"] ?? throw new InvalidOperationException(Messages.PublicJwksRequired);
                var publicKeys = new JsonWebKeySet(await File.ReadAllTextAsync(publicKeysPath));
                if (operation == "replace-client-keys")
                {
                    await registry.ReplacePublicKeysAsync(clientId, publicKeys);
                }
                else
                {
                    var contextId = builder.Configuration["context"];
                    await registry.RegisterAsync(clientId, contextId, publicKeys);
                }
            }
            Console.WriteLine(Messages.ClientAdministrationCompleted);
            return;
        }
        if (builder.Configuration.GetValue<bool>("prune-assertions"))
        {
            await using var scope = app.Services.CreateAsyncScope();
            var removed = await scope.ServiceProvider.GetRequiredService<AuthDbContext>().PruneExpiredAssertionsAsync();
            Console.WriteLine(Messages.ExpiredReplayMarkersRemoved(removed));
            return;
        }
        app.UseRequestLocalization(new RequestLocalizationOptions()
            .SetDefaultCulture("en")
            .AddSupportedCultures("en", "de")
            .AddSupportedUICultures("en", "de"));
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapBaseAuthTokenEndpoint();
        app.MapBaseAuthLocalUserAuthorization();
        app.MapStaticAssets();
        app.MapRazorPages().WithStaticAssets();
        app.MapGet("/api/ping", (HttpContext context) => TypedResults.Json(context.User.GetBaseAuthActor(), MachineServiceJsonContext.Default.Actor))
            .RequireAuthorization(BaseAuthPolicies.Service);
        app.MapGet("/api/me", (HttpContext context) => TypedResults.Json(context.User.GetBaseAuthActor(), MachineServiceJsonContext.Default.Actor))
            .RequireAuthorization(BaseAuthPolicies.User);
        await app.RunAsync();
    }

    private static X509Certificate2 loadCertificate(IConfiguration configuration, string purpose)
    {
        var path = configuration[$"Auth:{purpose}CertificatePath"]
            ?? throw new InvalidOperationException(Messages.CertificatePathRequired(purpose));
        return X509CertificateLoader.LoadPkcs12FromFile(path, configuration[$"Auth:{purpose}CertificatePassword"],
            X509KeyStorageFlags.EphemeralKeySet);
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Actor))]
internal sealed partial class MachineServiceJsonContext : JsonSerializerContext
{
}