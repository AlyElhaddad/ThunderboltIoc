namespace ThunderboltIoc;

public interface IThunderboltResolver : IServiceProvider
{
    /// <summary>
    /// Gets the specified service by its type.
    /// </summary>
    T Get<T>();
}
