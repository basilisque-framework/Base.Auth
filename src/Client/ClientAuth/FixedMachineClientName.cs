namespace Basilisque.Base.Auth.Client.ClientAuth;

internal sealed class FixedMachineClientName
{
    internal FixedMachineClientName(string value)
    {
        Value = value;
    }

    internal string Value { get; }
}
