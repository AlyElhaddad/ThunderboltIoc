namespace ThunderboltIoc;

public sealed class ThunderboltContainer : IThunderboltContainer, IThunderboltRegistry, IThunderboltResolver, IThunderboltRegistrar, IServiceProvider
{
    private class RegisterIfNotExistsRegistrar<TReg> : IThunderboltRegistrar
        where TReg : IThunderboltRegistrar, IThunderboltRegistry
    {
        private static RegisterIfNotExistsRegistrar<TReg>? instance;
        public static RegisterIfNotExistsRegistrar<TReg> GetInstance(TReg reg) => instance ??= new(reg);


        private readonly TReg reg;

        private RegisterIfNotExistsRegistrar(TReg reg)
        {
            this.reg = reg;
        }

        public void AddTransient<TService>()
        {
            if (!reg.Registers.ContainsKey(typeof(TService)))
                reg.AddTransient<TService>();
        }

        public void AddScoped<TService>()
        {
            if (!reg.Registers.ContainsKey(typeof(TService)))
                reg.AddScoped<TService>();
        }

        public void AddSingleton<TService>()
        {
            if (!reg.Registers.ContainsKey(typeof(TService)))
                reg.AddSingleton<TService>();
        }

        public void AddTransient<TService, TImpl>() where TImpl : TService
        {
            if (!reg.Registers.ContainsKey(typeof(TService)))
                reg.AddTransient<TService, TImpl>();
        }

        public void AddScoped<TService, TImpl>() where TImpl : TService
        {
            if (!reg.Registers.ContainsKey(typeof(TService)))
                reg.AddScoped<TService, TImpl>();
        }

        public void AddSingleton<TService, TImpl>() where TImpl : TService
        {
            if (!reg.Registers.ContainsKey(typeof(TService)))
                reg.AddSingleton<TService, TImpl>();
        }

        public void AddTransientFactory<TService>(Func<TService> factory)
        {
            if (!reg.Registers.ContainsKey(typeof(TService)))
                reg.AddTransientFactory(factory);
        }

        public void AddScopedFactory<TService>(Func<TService> factory)
        {
            if (!reg.Registers.ContainsKey(typeof(TService)))
                reg.AddScopedFactory(factory);
        }

        public void AddSingletonFactory<TService>(Func<TService> factory)
        {
            if (!reg.Registers.ContainsKey(typeof(TService)))
                reg.AddSingletonFactory(factory);
        }

        public void AddTransient<TService>(Func<Type> implSelector)
        {
            if (!reg.Registers.ContainsKey(typeof(TService)))
                reg.AddTransient<TService>(implSelector);
        }

        public void AddScoped<TService>(Func<Type> implSelector)
        {
            if (!reg.Registers.ContainsKey(typeof(TService)))
                reg.AddScoped<TService>(implSelector);
        }

        public void AddSingleton<TService>(Func<Type> implSelector)
        {
            if (!reg.Registers.ContainsKey(typeof(TService)))
                reg.AddSingleton<TService>(implSelector);
        }
    }

    private readonly Dictionary<Type, ThunderboltRegister> registers;
    private readonly ThunderboltScope singletonScope;
    private readonly HashSet<Type> attachedRegistrations;

    internal ThunderboltContainer()
    {
        registers = new()
        {
            { typeof(IThunderboltContainer), new IocRegister<IThunderboltContainer>(typeof(ThunderboltContainer), ThunderboltServiceLifetime.Singleton, null, factory: () => this) },
            { typeof(IThunderboltResolver), new IocRegister<IThunderboltResolver>(typeof(ThunderboltContainer), ThunderboltServiceLifetime.Singleton, null, factory: () => this) },
            { typeof(IThunderboltScope), new IocRegister<IThunderboltScope>(typeof(ThunderboltScope), ThunderboltServiceLifetime.Transient, null, factory: () => CreateScope()) },
        };
        singletonScope = new(this);
        attachedRegistrations = new();
    }

    IReadOnlyDictionary<Type, ThunderboltRegister> IThunderboltRegistry.Registers { get => registers; }

    public IThunderboltScope CreateScope()
    {
        return new ThunderboltScope(this);
    }

    public T Get<T>()
    {
        return InternalGet<T>(null);
    }

    internal T InternalGet<T>(ThunderboltScope? scope)
    {
        Type t = typeof(T);
        return (T)InternalGet(t, scope);
    }
    internal object InternalGet(Type type, ThunderboltScope? scope)
    {
        if (!registers.TryGetValue(type, out var register))
            throw new InvalidOperationException($"Unable to locate a registered implementation for a service of type '{type}'.");
        return (register.Lifetime, scope) switch
        {
            (ThunderboltServiceLifetime.Scoped, ThunderboltScope) => scope.GetScoped(type),
            (ThunderboltServiceLifetime.Singleton or ThunderboltServiceLifetime.Scoped, _) => singletonScope.GetScoped(type),
            (ThunderboltServiceLifetime.Transient, _) => Create(this, register),
            _ => throw new InvalidOperationException("Unknown ServiceLifetime.")
        };
    }
    internal object Create(Type type, IThunderboltResolver iocResolver)
    {
        if (!registers.TryGetValue(type, out var register))
            throw new InvalidOperationException($"Unable to locate a registered implementation for a service of type '{type}'.");
        return Create(iocResolver, register);
    }
    private static object Create(IThunderboltResolver iocResolver, ThunderboltRegister register)
    {
        if (register.Factory is not null) return register.Factory();
        if (register.ImplSelector is not null) return ThunderboltFactory.Instance.Create(iocResolver, register.ImplSelector());

        return ThunderboltFactory.Instance.Create(iocResolver, register.ImplType);
    }

    internal void Attach<TRegistration>()
        where TRegistration : notnull, ThunderboltRegistration, new()
    {
        Type regType = typeof(TRegistration);
        if (!attachedRegistrations.Add(regType))
            throw new InvalidOperationException($"A registration of type '{regType}' has already been attached. Registrations of the same type must not be registered multiple times.");
        TRegistration reg = new();
        reg.StaticRegister(RegisterIfNotExistsRegistrar<ThunderboltContainer>.GetInstance(this), ThunderboltFactory.Instance);
        reg.DictateServiceFactories(ThunderboltFactory.Instance);
        reg.Register(this);
        if (reg is IDisposable disposable)
            disposable.Dispose();
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
    }

    #endregion

    object IServiceProvider.GetService(Type serviceType)
    {
        return InternalGet(serviceType, null);
    }
}
