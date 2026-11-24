using Basilisque.Base.Auth.Server.Configuration;
using Basilisque.Base.Auth.Server.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Basilisque.Base.Auth.MachineService;

internal static class LocalUserAdministration
{
    internal static async Task<bool> TryRunAsync(IServiceProvider services, IConfiguration configuration)
    {
        var operationNames = new[] { "create-user", "reset-user-password", "disable-user", "enable-user", "register-user-client" };
        var selected = operationNames.Where(name => !string.IsNullOrWhiteSpace(configuration[name])).ToArray();
        if (selected.Length == 0)
            return false;
        if (selected.Length != 1 || new[] { "register-client", "replace-client-keys", "disable-client" }
            .Any(name => !string.IsNullOrWhiteSpace(configuration[name])))
        {
            throw new InvalidOperationException(Messages.OneAdministrationOperation);
        }

        var operation = selected[0];
        var identifier = configuration[operation]!;
        await using var scope = services.CreateAsyncScope();
        if (operation == "register-user-client")
        {
            var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
            var auth = scope.ServiceProvider.GetRequiredService<IOptions<AuthServerOptions>>().Value;
            await registerClientAsync(applications, auth, identifier, required(configuration, "redirect-uri"));
        }
        else
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>();
            var result = operation == "create-user"
                ? await users.CreateAsync(new LocalUser { UserName = identifier, Email = identifier, ContextId = configuration["context"] }, password())
                : await updateUserAsync(users, identifier, operation);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }
        Console.WriteLine(Messages.LocalAdministrationCompleted);
        return true;
    }

    private static async Task<IdentityResult> updateUserAsync(UserManager<LocalUser> users, string identifier, string operation)
    {
        var user = await users.FindByNameAsync(identifier) ?? throw new InvalidOperationException(Messages.UserNotFound);
        if (operation == "reset-user-password")
        {
            var token = await users.GeneratePasswordResetTokenAsync(user);
            return await users.ResetPasswordAsync(user, token, password());
        }
        user.Enabled = operation == "enable-user";
        return await users.UpdateSecurityStampAsync(user);
    }

    private static async Task registerClientAsync(IOpenIddictApplicationManager applications, AuthServerOptions auth, string clientId, string redirect)
    {
        if (!Uri.TryCreate(redirect, UriKind.Absolute, out var redirectUri) ||
            (redirectUri.Scheme != Uri.UriSchemeHttps && !(redirectUri.IsLoopback && redirectUri.Scheme == Uri.UriSchemeHttp)) ||
            !string.IsNullOrEmpty(redirectUri.Fragment) || !string.IsNullOrEmpty(redirectUri.UserInfo))
        {
            throw new InvalidOperationException(Messages.InvalidRedirectUri);
        }

        var client = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientType = ClientTypes.Public,
            ApplicationType = redirectUri.IsLoopback ? ApplicationTypes.Native : ApplicationTypes.Web,
            ConsentType = ConsentTypes.Implicit
        };
        client.RedirectUris.Add(redirectUri);
        client.Permissions.UnionWith([Permissions.Endpoints.Authorization, Permissions.Endpoints.Token,
            Permissions.GrantTypes.AuthorizationCode, Permissions.ResponseTypes.Code, Permissions.Prefixes.Scope + auth.ApiScope]);
        await applications.CreateAsync(client);
    }

    private static string required(IConfiguration configuration, string option)
        => configuration[option] ?? throw new InvalidOperationException(Messages.CommandOptionRequired(option));

    private static string password()
        => Environment.GetEnvironmentVariable("BASE_AUTH_LOCAL_USER_PASSWORD")
            ?? throw new InvalidOperationException(Messages.LocalUserPasswordRequired);
}