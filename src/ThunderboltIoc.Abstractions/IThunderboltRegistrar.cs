namespace ThunderboltIoc;

/// <summary>
/// Methods that are used to perform explicit services registrations.
/// </summary>
public interface IThunderboltRegistrar
{
    //register a service with itself as its implementation

    /// <summary>
    /// Registers a transient service by its type.
    /// </summary>
    void AddTransient<TService>() where TService : notnull;
    /// <summary>
    /// Registers a scoped service by its type.
    /// </summary>
    void AddScoped<TService>() where TService : notnull;
    /// <summary>
    /// Registers a singleton service by its type.
    /// </summary>
    void AddSingleton<TService>() where TService : notnull;



    //register a service that has a different type for its implementation

    /// <summary>
    /// Registers a transient service by its type and its implementation type.
    /// </summary>
    void AddTransient<TService, TImpl>() where TService : notnull where TImpl : notnull, TService;
    /// <summary>
    /// Registers a scoped service by its type and its implementation type.
    /// </summary>
    void AddScoped<TService, TImpl>() where TService : notnull where TImpl : notnull, TService;
    /// <summary>
    /// Registers a singleton service by its type and its implementation type.
    /// </summary>
    void AddSingleton<TService, TImpl>() where TService : notnull where TImpl : notnull, TService;



    //register a service that is created using a user-specified factory

    /// <summary>
    /// Registers a transient service using a factory.
    /// </summary>
    void AddTransientFactory<TService>(in Func<TService> factory) where TService : notnull;
    /// <summary>
    /// Registers a scoped service using a factory.
    /// </summary>
    void AddScopedFactory<TService>(in Func<TService> factory) where TService : notnull;
    /// <summary>
    /// Registers a singleton service using a factory.
    /// </summary>
    void AddSingletonFactory<TService>(in Func<TService> factory) where TService : notnull;



    //register a service which implementation is determined by a user-defined type selector

    /// <summary>
    /// Registers a transient service by its type with its implementation type determined by a type factory.
    /// </summary>
    void AddTransient<TService>(in Func<Type> implSelector) where TService : notnull;
    /// <summary>
    /// Registers a scoped service by its type with its implementation type determined by a type factory.
    /// </summary>
    void AddScoped<TService>(in Func<Type> implSelector) where TService : notnull;
    /// <summary>
    /// Registers a singleton service by its type with its implementation type determined by a type factory.
    /// </summary>
    void AddSingleton<TService>(in Func<Type> implSelector) where TService : notnull;
}
