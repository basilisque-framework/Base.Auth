using System.Globalization;
using System.Resources;

namespace Basilisque.Base.Auth.Abstractions;

internal static class Messages
{
    private static readonly ResourceManager _resources = new(typeof(Messages));

    internal static string ActorKindRequired => get(nameof(ActorKindRequired));

    internal static string ContextMustHaveValue => get(nameof(ContextMustHaveValue));

    internal static string SubjectRequired => get(nameof(SubjectRequired));

    internal static string TokenRequired => get(nameof(TokenRequired));

    private static string get(string key) => _resources.GetString(key, CultureInfo.CurrentUICulture)!;
}