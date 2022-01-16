using System.ComponentModel;

namespace ThunderboltIoc;

/// <summary>
/// This is for source generator purposes. Do not make use of that.
/// </summary>
/// <remarks>
/// What an evil factory should that be though :D
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[Browsable(false)]
public interface IThunderboltFactoryDictator
{
    /// <summary>
    /// This is for source generator purposes. Do not make use of that.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    void Dictate<T>(in Func<IThunderboltResolver, Func<Type>?, Func<T>?, T> factory) where T : notnull;
}
