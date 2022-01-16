namespace ThunderboltIoc;

/// <summary>
/// An <see cref="IThunderboltResolver"/> (usually, with a singleton implementation) that can also create scopes (via <see cref="CreateScope"/>).
/// </summary>
public interface IThunderboltContainer : IThunderboltResolver
{
    /// <summary>
    /// Creates and returns a new <see cref="IThunderboltScope"/>.
    /// </summary>
    IThunderboltScope CreateScope();
}
