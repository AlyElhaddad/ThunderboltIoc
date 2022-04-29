namespace ThunderboltIoc;

/// <summary>
/// An <see cref="IServiceProvider"/> that generically resolves (i.e gets) instances of the needed services.
/// </summary>
public interface IThunderboltResolver : IServiceProvider
{
    /// <summary>
    /// Gets the specified service by its type, or default value (null for reference types) if the service was not found.
    /// </summary>
    T? Get<T>() where T : notnull;
}