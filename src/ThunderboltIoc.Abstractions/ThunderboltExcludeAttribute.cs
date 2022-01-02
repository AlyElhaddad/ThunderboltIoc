namespace ThunderboltIoc;

/// <summary>
/// Used to exclude a particular type from the attribute registration process.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false)]
public class ThunderboltExcludeAttribute : Attribute
{
    public ThunderboltExcludeAttribute() : this(false) { }

    /// <param name="applyToDerivedTypes">Specifies whether or not to also exclude derived types.</param>
    public ThunderboltExcludeAttribute(bool applyToDerivedTypes)
    {
        ApplyToDerivedTypes = applyToDerivedTypes;
    }

    /// <summary>
    /// Specifies whether or not to also exclude derived types.
    /// </summary>
    public bool ApplyToDerivedTypes { get; }
}
