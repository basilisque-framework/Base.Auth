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

namespace Basilisque.Base.Auth.Authorization;

/// <summary>Defines the effect of an explicit role permission rule.</summary>
public enum PermissionEffect
{
    /// <summary>Allows an action in the absence of an applicable denial.</summary>
    Allow = 1,

    /// <summary>Denies an action even when a separate applicable rule allows it.</summary>
    Deny = 2
}