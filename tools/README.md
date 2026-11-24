<!--
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
-->
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