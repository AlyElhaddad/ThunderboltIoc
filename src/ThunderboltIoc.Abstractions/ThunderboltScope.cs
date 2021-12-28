namespace ThunderboltIoc;

internal sealed class ThunderboltScope : IThunderboltScope, IThunderboltResolver, IServiceProvider, IDisposable
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

    internal object GetScoped(Type type)
    {
        if (container is null || store is null)
            throw new ObjectDisposedException(GetType().FullName);
        if (store.TryGetValue(type, out var val))
        {
            return val;
        }
        else
        {
            object instance = container.Create(type, this);
            store?.Add(type, instance);
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

    object IServiceProvider.GetService(Type serviceType)
    {
        if (container is null || store is null)
            throw new ObjectDisposedException(GetType().FullName);
        return container.InternalGet(serviceType, this);
    }
}
