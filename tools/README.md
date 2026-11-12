# Verification tools

`Client.Credentials.DataProtection.Compatibility` verifies that .NET Framework 4.8 and .NET can exchange
protected private-key packages using a shared, host-managed Data Protection key ring.
It is a verification program, not a product library or deployment component.

Run the complete Windows compatibility check from the repository root:

```powershell
dotnet test --project src/Client.Integration.Tests -c Release
```

The TUnit integration tests also run the reference applications under `samples/` over HTTPS.
They build both framework variants of the helper programs, are skipped outside Windows,
and clean up their child processes and temporary credentials after each test.
