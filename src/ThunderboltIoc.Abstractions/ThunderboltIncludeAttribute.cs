namespace ThunderboltIoc;

/// <summary>
/// Includes the specified type in the attribute registration process.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false)]
public class ThunderboltIncludeAttribute : Attribute
{
    /// <param name="serviceLifetime">The <see cref="ThunderboltServiceLifetime"/> used to register the service(s).</param>
    public ThunderboltIncludeAttribute(ThunderboltServiceLifetime serviceLifetime)
    {
        ServiceLifetime = serviceLifetime;
    }
    /// <param name="serviceLifetime">The <see cref="ThunderboltServiceLifetime"/> used to register the service(s).</param>
    /// <param name="implementation">(optional) The type of the implementation to use.</param>
    public ThunderboltIncludeAttribute(ThunderboltServiceLifetime serviceLifetime, Type implementation) : this(serviceLifetime, implementation, false) { }

    /// <param name="serviceLifetime">The <see cref="ThunderboltServiceLifetime"/> used to register the service(s).</param>
    /// <param name="applyToDerivedTypes">(optional, default: false) Specifies whether or not derived types should be included.</param>
    public ThunderboltIncludeAttribute(ThunderboltServiceLifetime serviceLifetime, bool applyToDerivedTypes)
        : this(serviceLifetime)
    {
        ApplyToDerivedTypes = applyToDerivedTypes;
    }

    /// <param name="serviceLifetime">The <see cref="ThunderboltServiceLifetime"/> used to register the service(s).</param>
    /// <param name="implementation">(optional) The type of the implementation to use.</param>
    /// <param name="applyToDerivedTypes">(optional, default: false) Specifies whether or not derived types should be included.</param>
    public ThunderboltIncludeAttribute(ThunderboltServiceLifetime serviceLifetime, Type implementation, bool applyToDerivedTypes)
        : this(serviceLifetime, applyToDerivedTypes)
    {
        Implementation = implementation;
    }
    /// <summary>
    /// The <see cref="ThunderboltServiceLifetime"/> used to register the service(s).
    /// </summary>
    public ThunderboltServiceLifetime ServiceLifetime { get; }
    /// <summary>
    /// (optional) The type of the implementation to use.
    /// </summary>
    public Type? Implementation { get; }
    /// <summary>
    /// (optional, default: false) Specifies whether or not derived types should be included.
    /// </summary>
    public bool ApplyToDerivedTypes { get; }
}
