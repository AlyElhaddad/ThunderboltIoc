using System.ComponentModel;

namespace ThunderboltIoc;

/// <summary>
/// This is for source generator purposes. Do not make use of that.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[Browsable(false)]
public interface IThunderboltFactoryReflectionDictator : IThunderboltFactoryDictator
{
    /// <summary>
    /// This is for source generator purposes. Do not make use of that.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    void Dictate(in Type serviceType, in Func<IThunderboltResolver, Func<Type>?, Func<IThunderboltResolver, object>?, object> factory);

    /// <summary>
    /// This is for source generator purposes. Do not make use of that.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    void Dictate(in Type serviceType, in Func<IThunderboltResolver, Func<Type>?, Func<IThunderboltResolver, object>?, object> factory, ThunderboltServiceLifetime serviceLifetime, Func<Type>? implSelector, Func<IThunderboltResolver, object>? userFactory);
}
