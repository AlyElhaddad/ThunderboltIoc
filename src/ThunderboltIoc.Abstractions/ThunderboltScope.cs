namespace ThunderboltIoc;

internal sealed class ThunderboltScope : IThunderboltScope, IThunderboltResolver, IServiceProvider, IDisposable
{
    private static int globalId;

    private readonly int id;
    private ThunderboltContainer? container;

    internal ThunderboltScope(in ThunderboltContainer container)
    {
        id = Interlocked.Increment(ref globalId);
        this.container = container;
    }

    public int Id => id;

#pragma warning disable CS8604 // Possible null reference argument.
    public T Get<T>() where T : notnull
    {
        if (disposed)
            throw new ObjectDisposedException(GetType().FullName);

        return ThunderboltServiceRegistry<T>.RegisteredLifetime switch
        {
            ThunderboltServiceLifetime.Transient => ThunderboltServiceRegistry<T>.Factory(this, ThunderboltServiceRegistry<T>.RegisteredImplSelector, ThunderboltServiceRegistry<T>.RegisteredUserFactory),
            ThunderboltServiceLifetime.Scoped
                => ThunderboltServiceRegistry<T>.scopesInstances.TryGetValue(id, out T? val)
                ? val
                : ThunderboltServiceRegistry<T>.SetInstance(id, ThunderboltServiceRegistry<T>.Factory(this, ThunderboltServiceRegistry<T>.RegisteredImplSelector, ThunderboltServiceRegistry<T>.RegisteredUserFactory)),
            ThunderboltServiceLifetime.Singleton => ThunderboltServiceRegistry<T>.GetSingleton(container),
            _ => throw new InvalidOperationException("Unknown ThunderboltServiceLifetime."),
        };
    }
    public object GetService(Type serviceType)
    {
        if (disposed)
            throw new ObjectDisposedException(GetType().FullName);
        var genAcc = ThunderboltServiceRegistry.generic[serviceType];
        return genAcc.lifetimeGetter() switch
        {
            ThunderboltServiceLifetime.Transient => genAcc.factory(this),
            ThunderboltServiceLifetime.Scoped => genAcc.scopesInstances.TryGetValue(id, out object? val)
                ? val
                : genAcc.instanceSetter(id, genAcc.factory(this)),
            ThunderboltServiceLifetime.Singleton => genAcc.singletonInstanceGetter(container),
            _ => throw new InvalidOperationException("Unknown ThunderboltServiceLifetime.")
        };
    }
#pragma warning restore CS8604 // Possible null reference argument.

    #region IDisposable pattern
    private bool disposed;
    private void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                // Should: dispose scoped instances
            }

            if (ThunderboltServiceRegistry.scopeClearanceActions.TryGetValue(id, out var actions))
            {
                if (actions is not null)
                {
                    Parallel.ForEach(actions, action => action());
                    actions.Clear();
                }
                ThunderboltServiceRegistry.scopeClearanceActions.Remove(id);
            }
            container = null;
            disposed = true;
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

