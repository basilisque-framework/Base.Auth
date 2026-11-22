using Microsoft.Extensions.Options;

namespace Basilisque.Base.Auth.Server.Configuration;

internal sealed class AuthServerOptionsValidator : IValidateOptions<AuthServerOptions>
{
    public ValidateOptionsResult Validate(string? name, AuthServerOptions options)
        => name != Options.DefaultName ? ValidateOptionsResult.Skip : options.IsValid()
            ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(Messages.InvalidAuthServerOptions);
}
