namespace ThunderboltIoc;

internal sealed class ThunderboltScope : IThunderboltScope, IThunderboltResolver, IDisposable
{
    private Dictionary<Type, object>? store;
    private ThunderboltContainer? container;

    internal ThunderboltScope(ThunderboltContainer container)
    {
        Id = Guid.NewGuid();
        store = new();
        this.container = container;
    }

    public Guid Id { get; }
    public T Get<T>()
    {
        if (container is null || store is null)
            throw new ObjectDisposedException(GetType().FullName);
        return container.InternalGet<T>(this);
    }
    internal T GetScoped<T>()
    {
        if (container is null || store is null)
            throw new ObjectDisposedException(GetType().FullName);
        Type t = typeof(T);
        if (store.TryGetValue(t, out var val))
        {
            return (T)val;
        }
        else
        {
            T instance = container.Create<T>(this);
#pragma warning disable CS8604 // Possible null reference argument.
            store?.Add(t, instance);
#pragma warning restore CS8604 // Possible null reference argument.
            return instance;
        }
    }

    #region IDisposable pattern
    private bool disposedValue;
    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            store?.Clear();
            store = null;
            container = null;
            disposedValue = true;
        }
    }
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
