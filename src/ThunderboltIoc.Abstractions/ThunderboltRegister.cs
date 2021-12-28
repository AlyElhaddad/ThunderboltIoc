namespace ThunderboltIoc;

public record ThunderboltRegister
{
    private protected ThunderboltRegister(Type serviceType, Type? implType, ThunderboltServiceLifetime lifetime, Func<Type>? implSelector, Func<object>? factory)
    {
        ServiceType = serviceType;
        ImplType = implType ?? serviceType;
        Lifetime = lifetime;
        ImplSelector = implSelector;
        Factory = factory;
    }

    public Type ServiceType { get; }
    public Type ImplType { get; }
    public ThunderboltServiceLifetime Lifetime { get; }
    public Func<Type>? ImplSelector { get; }
    public Func<object>? Factory { get; }
}

public record IocRegister<TService> : ThunderboltRegister
{
#pragma warning disable CS8603 // Possible null reference return.
    internal IocRegister(Type? implType, ThunderboltServiceLifetime lifetime, Func<Type>? implSelector, Func<TService>? factory)
        : base(typeof(TService), implType, lifetime, implSelector, factory is null ? null : () => factory())
#pragma warning restore CS8603 // Possible null reference return.
    {
    }

    public new Func<TService>? Factory { get => base.Factory is null ? null : () => (TService)base.Factory(); }
}
