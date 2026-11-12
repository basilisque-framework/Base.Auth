#if NET48
namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill for C# required members support on older frameworks.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class RequiredMemberAttribute : Attribute
{
}

/// <summary>
/// Polyfill for compiler-required feature metadata.
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
internal sealed class CompilerFeatureRequiredAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompilerFeatureRequiredAttribute"/> class.
    /// </summary>
    /// <param name="featureName">The compiler feature name.</param>
    public CompilerFeatureRequiredAttribute(string featureName)
    {
        FeatureName = featureName;
    }

    /// <summary>
    /// Gets the compiler feature name.
    /// </summary>
    public string FeatureName { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the feature is optional.
    /// </summary>
    public bool IsOptional { get; set; }

    /// <summary>
    /// Gets the required-members feature identifier.
    /// </summary>
    public const string RequiredMembers = nameof(RequiredMembers);
}

/// <summary>
/// Polyfill for constructors that set all required members.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
internal sealed class SetsRequiredMembersAttribute : Attribute
{
}
#endif
