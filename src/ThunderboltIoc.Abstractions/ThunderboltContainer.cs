namespace ThunderboltIoc;

internal sealed class ThunderboltContainer : IThunderboltContainer, IThunderboltResolver, IThunderboltRegistrar, IThunderboltFactoryDictator, IServiceProvider
{
    internal ThunderboltContainer()
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        ThunderboltServiceRegistry<IThunderboltContainer>.Dictate((_, _, userFactory) => userFactory());
        ThunderboltServiceRegistry<IThunderboltScope>.Dictate((_, _, userFactory) => userFactory());
        ThunderboltServiceRegistry<IThunderboltResolver>.Dictate((resolver, _, _) => resolver);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        ThunderboltServiceRegistry<IThunderboltContainer>.Register(ThunderboltServiceLifetime.Singleton, () => this);
        ThunderboltServiceRegistry<IThunderboltScope>.Register(ThunderboltServiceLifetime.Transient, () => CreateScope());
        ThunderboltServiceRegistry<IThunderboltResolver>.Register(ThunderboltServiceLifetime.Transient);
    }

    internal void Attach<TRegistration>()
        where TRegistration : notnull, ThunderboltRegistration, new()
    {
        TRegistration reg = new();
        reg.DictateServiceFactories(this);
        reg.StaticRegister(this);
        reg.Register(this);
    }

    public IThunderboltScope CreateScope()
        => new ThunderboltScope(this);

    public T Get<T>() where T : notnull
    {
        return ThunderboltServiceRegistry<T>.RegisteredLifetime switch
        {
            ThunderboltServiceLifetime.Transient => ThunderboltServiceRegistry<T>.Factory(this, ThunderboltServiceRegistry<T>.RegisteredImplSelector, ThunderboltServiceRegistry<T>.RegisteredUserFactory),
            ThunderboltServiceLifetime.Scoped or ThunderboltServiceLifetime.Singleton => ThunderboltServiceRegistry<T>.GetSingleton(this),
            _ => throw new InvalidOperationException("Unknown ThunderboltServiceLifetime."),
        };
    }
    public object GetService(Type serviceType)
    {
        var genAcc = ThunderboltServiceRegistry.generic[serviceType];
        //var (lifetimeGetter, factory, _, singletonInstanceGetter, _) = ThunderboltServiceRegistry.generic[serviceType];
        return genAcc.lifetimeGetter() switch
        {
            ThunderboltServiceLifetime.Transient => genAcc.factory(this),
            ThunderboltServiceLifetime.Scoped or ThunderboltServiceLifetime.Singleton => genAcc.singletonInstanceGetter(this),
            _ => throw new InvalidOperationException("Unknown ThunderboltServiceLifetime."),
        };
    }

    #region IThunderboltRegistrar
    public void AddTransient<TService>() where TService : notnull
        => ThunderboltServiceRegistry<TService>.Register(ThunderboltServiceLifetime.Transient);
    public void AddScoped<TService>() where TService : notnull
        => ThunderboltServiceRegistry<TService>.Register(ThunderboltServiceLifetime.Scoped);
    public void AddSingleton<TService>() where TService : notnull
        => ThunderboltServiceRegistry<TService>.Register(ThunderboltServiceLifetime.Singleton);

    public void AddTransient<TService, TImpl>() where TService : notnull where TImpl : notnull, TService
        => ThunderboltServiceRegistry<TService>.Register(ThunderboltServiceLifetime.Transient);
    public void AddScoped<TService, TImpl>() where TService : notnull where TImpl : notnull, TService
        => ThunderboltServiceRegistry<TService>.Register(ThunderboltServiceLifetime.Scoped);
    public void AddSingleton<TService, TImpl>() where TService : notnull where TImpl : notnull, TService
        => ThunderboltServiceRegistry<TService>.Register(ThunderboltServiceLifetime.Singleton);

    public void AddTransientFactory<TService>(in Func<TService> factory) where TService : notnull
        => ThunderboltServiceRegistry<TService>.Register(ThunderboltServiceLifetime.Transient, factory);
    public void AddScopedFactory<TService>(in Func<TService> factory) where TService : notnull
        => ThunderboltServiceRegistry<TService>.Register(ThunderboltServiceLifetime.Scoped, factory);
    public void AddSingletonFactory<TService>(in Func<TService> factory) where TService : notnull
        => ThunderboltServiceRegistry<TService>.Register(ThunderboltServiceLifetime.Singleton, factory);

    public void AddTransient<TService>(in Func<Type> implSelector) where TService : notnull
        => ThunderboltServiceRegistry<TService>.Register(ThunderboltServiceLifetime.Transient, implSelector);
    public void AddScoped<TService>(in Func<Type> implSelector) where TService : notnull
        => ThunderboltServiceRegistry<TService>.Register(ThunderboltServiceLifetime.Scoped, implSelector);
    public void AddSingleton<TService>(in Func<Type> implSelector) where TService : notnull
        => ThunderboltServiceRegistry<TService>.Register(ThunderboltServiceLifetime.Singleton, implSelector);
    #endregion

    public void Dictate<T>(in Func<IThunderboltResolver, Func<Type>?, Func<T>?, T> factory) where T : notnull
        => ThunderboltServiceRegistry<T>.Dictate(factory);
}
