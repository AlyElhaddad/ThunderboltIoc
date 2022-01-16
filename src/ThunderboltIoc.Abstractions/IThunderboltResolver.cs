namespace ThunderboltIoc;

/// <summary>
/// An <see cref="IServiceProvider"/> that generically resolves (i.e gets) instances of the required services.
/// </summary>
public interface IThunderboltResolver : IServiceProvider
{
    /// <summary>
    /// Gets the specified service by its type.
    /// </summary>
    T Get<T>() where T : notnull;
}
