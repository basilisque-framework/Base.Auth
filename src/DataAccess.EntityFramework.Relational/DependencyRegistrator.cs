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


using Basilisque.DataAccess.EntityFramework.Base.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.Base.Auth.DataAccess.EntityFramework.Relational;

/// <summary>
/// Includes the Basilisque relational infrastructure in generated dependency registration.
/// </summary>
public partial class DependencyRegistrator
{
    partial void doAfterRegistration(IServiceCollection services)
    {
        // The IDbInitializer annotation also discovers the open generic adapter.
        // Persistence registration adds only the closed initializers for enabled contexts.
        var generated = services.FirstOrDefault(service => service.ServiceType == typeof(IDbInitializer) &&
            service.ImplementationType == typeof(AuthPersistenceExtensions.MigrationInitializer<>));
        if (generated is not null)
            services.Remove(generated);
    }
}