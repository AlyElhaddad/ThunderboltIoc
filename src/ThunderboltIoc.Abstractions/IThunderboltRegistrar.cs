namespace ThunderboltIoc;

public interface IThunderboltRegistrar
{
    //register a service with itself as its implementation

    /// <summary>
    /// Registers a transient service by its type.
    /// </summary>
    void AddTransient<TService>();
    /// <summary>
    /// Registers a scoped service by its type.
    /// </summary>
    void AddScoped<TService>();
    /// <summary>
    /// Registers a singleton service by its type.
    /// </summary>
    void AddSingleton<TService>();



    //register a service that has a different type for its implementation

    /// <summary>
    /// Registers a transient service by its type and its implementation type.
    /// </summary>
    void AddTransient<TService, TImpl>() where TImpl : TService;
    /// <summary>
    /// Registers a scoped service by its type and its implementation type.
    /// </summary>
    void AddScoped<TService, TImpl>() where TImpl : TService;
    /// <summary>
    /// Registers a singleton service by its type and its implementation type.
    /// </summary>
    void AddSingleton<TService, TImpl>() where TImpl : TService;



    //register a service that is created using a user-specified factory

    /// <summary>
    /// Registers a transient service using a factory.
    /// </summary>
    void AddTransientFactory<TService>(Func<TService> factory);
    /// <summary>
    /// Registers a scoped service using a factory.
    /// </summary>
    void AddScopedFactory<TService>(Func<TService> factory);
    /// <summary>
    /// Registers a singleton service using a factory.
    /// </summary>
    void AddSingletonFactory<TService>(Func<TService> factory);



    //register a service which implementation is determined by a user-defined type selector

    /// <summary>
    /// Registers a transient service by its type with its implementation type determined by a type factory.
    /// </summary>
    void AddTransient<TService>(Func<Type> implSelector);
    /// <summary>
    /// Registers a scoped service by its type with its implementation type determined by a type factory.
    /// </summary>
    void AddScoped<TService>(Func<Type> implSelector);
    /// <summary>
    /// Registers a singleton service by its type with its implementation type determined by a type factory.
    /// </summary>
    void AddSingleton<TService>(Func<Type> implSelector);
}
