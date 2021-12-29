using System.ComponentModel;

namespace ThunderboltIoc;

public abstract class ThunderboltRegistration
{
    /// <summary>
    /// This is for source generator purposes. Do not make use of that.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    internal protected virtual void DictateServiceFactories(IThunderboltFactoryDictator dictator) { }

    /// <summary>
    /// This is for source generator purposes. Do not make use of that.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    internal protected virtual void StaticRegister(IThunderboltRegistrar reg) { }

    /// <summary>
    /// This is where you register your services expilictly.
    /// </summary>
    /// <param name="reg">The <see cref="IThunderboltRegistrar"/> used to register your services.</param>
    public abstract void Register(IThunderboltRegistrar reg);
}
