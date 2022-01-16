namespace ThunderboltIoc;

/// <summary>
/// An <see cref="IThunderboltResolver"/> that can have unique (per <see cref="IThunderboltScope"/>) instances registered with the <see cref="ThunderboltServiceLifetime.Scoped"/> lifetime.
/// </summary>
public interface IThunderboltScope : IThunderboltResolver, IDisposable
{
    /// <summary>
    /// Used distinguish scopes.
    /// </summary>
    int Id { get; }
}
