namespace ThunderboltIoc;

public interface IThunderboltRegistrar
{
    //register a service with itself as its implementation
    void AddTransient<TService>();
    void AddScoped<TService>();
    void AddSingleton<TService>();

    //register a service that has a different type for its implementation
    void AddTransient<TService, TImpl>() where TImpl : TService;
    void AddScoped<TService, TImpl>() where TImpl : TService;
    void AddSingleton<TService, TImpl>() where TImpl : TService;

    //register a service that is created using a user-specified factory
    void AddTransientFactory<TService>(Func<TService> factory);
    void AddScopedFactory<TService>(Func<TService> factory);
    void AddSingletonFactory<TService>(Func<TService> factory);

    //register a service which implementation is determined by a user-defined type selector
    void AddTransient<TService>(Func<Type> implSelector);
    void AddScoped<TService>(Func<Type> implSelector);
    void AddSingleton<TService>(Func<Type> implSelector);

}
