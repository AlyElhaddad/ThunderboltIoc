namespace ThunderboltIoc;

public abstract partial class ThunderboltContainer : IThunderboltContainer, IThunderboltRegistry, IThunderboltResolver, IThunderboltRegistrar
{
    private readonly Dictionary<Type, ThunderboltRegister> registers;
    private readonly ThunderboltScope singletonScope;

    protected ThunderboltContainer()
    {
        registers = new()
        {
            { typeof(IThunderboltContainer), new IocRegister<IThunderboltContainer>(typeof(ThunderboltContainer), ThunderboltServiceLifetime.Singleton, null, factory: () => this) },
            { typeof(IThunderboltResolver), new IocRegister<IThunderboltResolver>(typeof(ThunderboltContainer), ThunderboltServiceLifetime.Singleton, null, factory: () => this) },
            { typeof(IThunderboltScope), new IocRegister<IThunderboltScope>(typeof(ThunderboltScope), ThunderboltServiceLifetime.Transient, null, factory: () => CreateScope()) },
        };
        singletonScope = new(this);
        StaticRegister(this);
        Register(this);
    }

    partial void StaticRegister(IThunderboltRegistrar reg);
    public abstract void Register(IThunderboltRegistrar reg);

    IReadOnlyDictionary<Type, ThunderboltRegister> IThunderboltRegistry.Registers { get => registers; }

    public IThunderboltScope CreateScope()
    {
        return new ThunderboltScope(this);
    }

    internal T InternalGet<T>(ThunderboltScope? scope)
    {
        Type t = typeof(T);
#pragma warning disable CS8604 // Possible null reference argument.
        if (!registers.TryGetValue(t, out var register))
            throw new InvalidOperationException($"Unable to locate a registered implementation for a service of type '{t}'.");
        return (register.Lifetime, scope) switch
        {
            (ThunderboltServiceLifetime.Scoped, ThunderboltScope) => scope.GetScoped<T>(),
            (ThunderboltServiceLifetime.Singleton or ThunderboltServiceLifetime.Scoped, _) => singletonScope.GetScoped<T>(),
            (ThunderboltServiceLifetime.Transient, _) => (T)Create(this, register),
            _ => throw new InvalidOperationException("Unknown ServiceLifetime.")
        };
    }

    public T Get<T>()
    {
        return InternalGet<T>(null);
    }

    internal T Create<T>(IThunderboltResolver iocResolver)
    {
        Type t = typeof(T);
        if (!registers.TryGetValue(t, out var register))
            throw new InvalidOperationException($"Unable to locate a registered implementation for a service of type '{t}'.");
        return (T)Create(iocResolver, register);
    }
    private static object Create(IThunderboltResolver iocResolver, ThunderboltRegister register)
    {
        if (register.Factory is not null) return register.Factory();
        if (register.ImplSelector is not null) return ThunderboltFactory.Create(iocResolver, register.ImplSelector());

        return ThunderboltFactory.Create(iocResolver, register.ImplType);
    }

    #region IIocRegistrar
    void IThunderboltRegistrar.AddTransient<TService>()
    {
        Type serviceImpl = typeof(TService);
        registers.Add(serviceImpl, new IocRegister<TService>(serviceImpl, ThunderboltServiceLifetime.Transient, null, null));
    }

    void IThunderboltRegistrar.AddScoped<TService>()
    {
        Type serviceImpl = typeof(TService);
        registers.Add(serviceImpl, new IocRegister<TService>(serviceImpl, ThunderboltServiceLifetime.Scoped, null, null));
    }

    void IThunderboltRegistrar.AddSingleton<TService>()
    {
        Type serviceImpl = typeof(TService);
        registers.Add(serviceImpl, new IocRegister<TService>(serviceImpl, ThunderboltServiceLifetime.Singleton, null, null));
    }

    void IThunderboltRegistrar.AddTransient<TService, TImpl>()
    {
        registers.Add(typeof(TService), new IocRegister<TService>(typeof(TImpl), ThunderboltServiceLifetime.Transient, null, null));
    }

    void IThunderboltRegistrar.AddScoped<TService, TImpl>()
    {
        registers.Add(typeof(TService), new IocRegister<TService>(typeof(TImpl), ThunderboltServiceLifetime.Scoped, null, null));
    }

    void IThunderboltRegistrar.AddSingleton<TService, TImpl>()
    {
        registers.Add(typeof(TService), new IocRegister<TService>(typeof(TImpl), ThunderboltServiceLifetime.Singleton, null, null));
    }

    void IThunderboltRegistrar.AddTransientFactory<TService>(Func<TService> factory)
    {
        Type serviceImpl = typeof(TService);
        registers.Add(serviceImpl, new IocRegister<TService>(serviceImpl, ThunderboltServiceLifetime.Transient, null, factory: factory));
    }

    void IThunderboltRegistrar.AddScopedFactory<TService>(Func<TService> factory)
    {
        Type serviceImpl = typeof(TService);
        registers.Add(serviceImpl, new IocRegister<TService>(serviceImpl, ThunderboltServiceLifetime.Scoped, null, factory: factory));
    }

    void IThunderboltRegistrar.AddSingletonFactory<TService>(Func<TService> factory)
    {
        Type serviceImpl = typeof(TService);
        registers.Add(serviceImpl, new IocRegister<TService>(serviceImpl, ThunderboltServiceLifetime.Singleton, null, factory: factory));
    }

    //void IIocRegistrar.AddTransient<TService, TImpl>(Func<TImpl> factory)
    //{
    //    registers.Add(typeof(TService).AssemblyQualifiedName, new IocRegister<TService>(typeof(TImpl), ServiceLifetime.Transient, null, factory is null ? null : () => (TService)factory()));
    //}

    //void IIocRegistrar.AddScoped<TService, TImpl>(Func<TImpl> factory)
    //{
    //    registers.Add(typeof(TService).AssemblyQualifiedName, new IocRegister<TService>(typeof(TImpl), ServiceLifetime.Scoped, null, factory is null ? null : () => (TService)factory()));
    //}

    //void IIocRegistrar.AddSingleton<TService, TImpl>(Func<TImpl> factory)
    //{
    //    registers.Add(typeof(TService).AssemblyQualifiedName, new IocRegister<TService>(typeof(TImpl), ServiceLifetime.Singleton, null, factory is null ? null : () => (TService)factory()));
    //}

    void IThunderboltRegistrar.AddTransient<TService>(Func<Type> implSelector)
    {
        registers.Add(typeof(TService), new IocRegister<TService>(null, ThunderboltServiceLifetime.Transient, implSelector, null));
    }
    void IThunderboltRegistrar.AddScoped<TService>(Func<Type> implSelector)
    {
        registers.Add(typeof(TService), new IocRegister<TService>(null, ThunderboltServiceLifetime.Scoped, implSelector, null));
    }

    void IThunderboltRegistrar.AddSingleton<TService>(Func<Type> implSelector)
    {
        registers.Add(typeof(TService), new IocRegister<TService>(null, ThunderboltServiceLifetime.Singleton, implSelector, null));
#pragma warning restore CS8604 // Possible null reference argument.
    }
    #endregion

}
