namespace ThunderboltIoc;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
public class ThunderboltExcludeAttribute : Attribute
{
    public ThunderboltExcludeAttribute() : this(false) { }
    public ThunderboltExcludeAttribute(bool applyToDerivedTypes)
    {
        ApplyToDerivedTypes = applyToDerivedTypes;
    }
    public bool ApplyToDerivedTypes { get; }
}
