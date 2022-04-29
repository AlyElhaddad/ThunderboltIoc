using System.ComponentModel;

namespace ThunderboltIoc;

/// <summary>
/// This is for source generator purposes. Do not make use of that.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[Browsable(false)]
public interface IThunderboltReflectionRegistrar : IThunderboltRegistrar
{
    //register a service with itself as its implementation

    /// <summary>
    /// This is for source generator purposes. Do not make use of that.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    void AddTransientReflection(in Type serviceType);
    /// <summary>
    /// This is for source generator purposes. Do not make use of that.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    void AddScopedReflection(in Type serviceType);
    /// <summary>
    /// This is for source generator purposes. Do not make use of that.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    void AddSingletonReflection(in Type serviceType);



    //register a service that is created using a user-specified factory

    /// <summary>
    /// This is for source generator purposes. Do not make use of that.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    void AddTransientFactoryReflection(in Type serviceType, in Func<IThunderboltResolver, object> factory);
    /// <summary>
    /// This is for source generator purposes. Do not make use of that.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    void AddScopedFactoryReflection(in Type serviceType, in Func<IThunderboltResolver, object> factory);
    /// <summary>
    /// This is for source generator purposes. Do not make use of that.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    void AddSingletonFactoryReflection(in Type serviceType, in Func<IThunderboltResolver, object> factory);
}
