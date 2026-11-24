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

namespace Basilisque.Base.Auth.Abstractions.Identity;

/// <summary>
/// A product identity independent of the authentication protocol or provider.
/// </summary>
public sealed class Actor
{
    /// <summary>
    /// Gets the stable internal subject identifier.
    /// </summary>
    public string SubjectId { get; }

    /// <summary>
    /// Gets the actor kind, either service or user.
    /// </summary>
    public string Kind { get; }

    /// <summary>
    /// Gets the server-assigned installation or customer context, if assigned.
    /// </summary>
    public string? ContextId { get; }

    /// <summary>
    /// Creates an actor with an optional application context.
    /// </summary>
    public Actor(string subjectId, string kind, string? contextId = null)
    {
        if (string.IsNullOrWhiteSpace(subjectId))
            throw new ArgumentException(Messages.SubjectRequired, nameof(subjectId));
        if (string.IsNullOrWhiteSpace(kind))
            throw new ArgumentException(Messages.ActorKindRequired, nameof(kind));
        if (contextId is not null && string.IsNullOrWhiteSpace(contextId))
            throw new ArgumentException(Messages.ContextMustHaveValue, nameof(contextId));

        SubjectId = subjectId;
        Kind = kind;
        ContextId = contextId;
    }
}