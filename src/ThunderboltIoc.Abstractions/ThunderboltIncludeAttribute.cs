namespace ThunderboltIoc;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false)]
public class ThunderboltIncludeAttribute : Attribute
{
    public ThunderboltIncludeAttribute(ThunderboltServiceLifetime serviceLifetime)
    {
        ServiceLifetime = serviceLifetime;
    }
    public ThunderboltIncludeAttribute(ThunderboltServiceLifetime serviceLifetime, Type implementation) : this(serviceLifetime, implementation, false) { }
    public ThunderboltIncludeAttribute(ThunderboltServiceLifetime serviceLifetime, bool applyToDerivedTypes)
        : this(serviceLifetime)
    {
        ApplyToDerivedTypes = applyToDerivedTypes;
    }
    public ThunderboltIncludeAttribute(ThunderboltServiceLifetime serviceLifetime, Type implementation, bool applyToDerivedTypes)
        : this(serviceLifetime, applyToDerivedTypes)
    {
        Implementation = implementation;
    }
    public ThunderboltServiceLifetime ServiceLifetime { get; }
    public Type? Implementation { get; }
    public bool ApplyToDerivedTypes { get; }
}
