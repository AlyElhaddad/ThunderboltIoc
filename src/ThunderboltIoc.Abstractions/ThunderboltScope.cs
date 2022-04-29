using Thunderbolt.GeneratorAbstractions;

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

    public T? Get<T>() where T : notnull
    {
        if (typeof(T).ToString().StartsWith("Swashbuckle"))
        {

        }
        if (disposed)
            throw new ObjectDisposedException(GetType().FullName);

        return ThunderboltServiceRegistry<T>.RegisteredLifetime switch
        {
            ThunderboltServiceLifetime.Transient => ThunderboltServiceRegistry<T>.Factory(this, ThunderboltServiceRegistry<T>.RegisteredImplSelector, ThunderboltServiceRegistry<T>.RegisteredUserFactory),
            ThunderboltServiceLifetime.Scoped
                => ThunderboltServiceRegistry<T>.scopesInstances.TryGetValue(id, out T? val)
                ? val
                : ThunderboltServiceRegistry<T>.SetInstance(id, ThunderboltServiceRegistry<T>.Factory(this, ThunderboltServiceRegistry<T>.RegisteredImplSelector, ThunderboltServiceRegistry<T>.RegisteredUserFactory)),
            ThunderboltServiceLifetime.Singleton => ThunderboltServiceRegistry<T>.GetSingleton(container!),
            _ => typeof(T).IsEnumerable() ? (T)typeof(T).EmptyEnumerable() : default!
        };
    }
    public object? GetService(Type serviceType)
    {
        if (serviceType.ToString().StartsWith("Swashbuckle"))
        {

        }
        if (disposed)
            throw new ObjectDisposedException(GetType().FullName);

        ThunderboltServiceLifetime? lifetime;
        if (ThunderboltServiceRegistry.generic.TryGetValue(serviceType, out var genAcc))
        {
            lifetime = genAcc.lifetimeGetter();
        }
        else
        {
            lifetime = ThunderboltServiceRegistry.InitializeAndGetServiceLifetime(serviceType);
            ThunderboltServiceRegistry.generic.TryGetValue(serviceType, out genAcc);
        }
        return lifetime switch
        {
            ThunderboltServiceLifetime.Transient => genAcc.factory(this),
            ThunderboltServiceLifetime.Scoped => genAcc.scopesInstances.TryGetValue(id, out object? val)
                ? val
                : genAcc.instanceSetter(id, genAcc.factory(this)),
            ThunderboltServiceLifetime.Singleton => genAcc.singletonInstanceGetter(container!),
            _ => serviceType.IsEnumerable() ? serviceType.EmptyEnumerable() : default
        };
    }

    #region IDisposable pattern
    private bool disposed;
    private void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                if (ThunderboltServiceRegistry.scopeClearanceActions.TryGetValue(id, out var actions))
                {
                    if (actions is not null)
                    {
                        Parallel.ForEach(actions, action => action());
                        actions.Clear();
                    }
                    ThunderboltServiceRegistry.scopeClearanceActions.Remove(id);
                }
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

