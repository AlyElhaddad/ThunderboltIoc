using System.ComponentModel;

namespace ThunderboltIoc;

public abstract class ThunderboltRegistration
{
    private static IReadOnlyDictionary<string, PrivateType>? privateTypes;
    /// <summary>
    /// This is for source generator purposes. Do not make use of that.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    internal protected static IReadOnlyDictionary<string, PrivateType> PrivateTypes => privateTypes ??= new Dictionary<string, PrivateType>();

    /// <summary>
    /// This is for source generator purposes. Do not make use of that.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    internal protected virtual void GeneratedRegistration<T>(T reg)
        where T : class, IThunderboltRegistrar, IThunderboltFactoryDictator, IThunderboltReflectionRegistrar, IThunderboltFactoryReflectionDictator
    { }

    /// <summary>
    /// This is where you register your services expilictly.
    /// </summary>
    /// <param name="reg">The <see cref="IThunderboltRegistrar"/> used to register your services.</param>
    internal protected abstract void Register(IThunderboltRegistrar reg);
}
