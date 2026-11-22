using Microsoft.Extensions.Options;

namespace Basilisque.Base.Auth.Client.ClientAuth;

internal sealed class MachineClientOptionsValidator : IValidateOptions<MachineClientOptions>
{
    private readonly string _registrationName;

    public MachineClientOptionsValidator(string registrationName)
    {
        _registrationName = registrationName;
    }

    public ValidateOptionsResult Validate(string? name, MachineClientOptions options)
    {
        if (name != _registrationName)
            return ValidateOptionsResult.Skip;

        return options.IsValid() ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(Messages.InvalidMachineClientOptions);
    }
}
