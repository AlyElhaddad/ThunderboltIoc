namespace ThunderboltIoc;

public interface IThunderboltScope : IThunderboltResolver, IDisposable
{
    /// <summary>
    /// Used distinguish scopes.
    /// </summary>
    Guid Id { get; }
}
