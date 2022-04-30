using System.Linq;

using Thunderbolt.GeneratorAbstractions;

namespace ThunderboltIoc;

internal class ThunderboltContainer : IThunderboltContainer, IThunderboltResolver, IThunderboltRegistrar, IThunderboltReflectionRegistrar, IThunderboltFactoryDictator, IThunderboltFactoryReflectionDictator, IServiceProvider
{
    internal ThunderboltContainer()
    {
        ThunderboltServiceRegistry<IThunderboltContainer>.Dictate((resolver, _, userFactory) => userFactory!(null!), ThunderboltServiceLifetime.Singleton, null, _ => this);
        ThunderboltServiceRegistry<IThunderboltScope>.Dictate((resolver, _, userFactory) => userFactory!(null!), ThunderboltServiceLifetime.Transient, null, _ => CreateScope());
        ThunderboltServiceRegistry<IThunderboltResolver>.Dictate((resolver, _, _) => resolver, ThunderboltServiceLifetime.Transient, null, null);
        ThunderboltServiceRegistry<IServiceProvider>.Dictate((resolver, _, _) => resolver, ThunderboltServiceLifetime.Transient, null, null);
    }

    internal void Attach<TRegistration>()
        where TRegistration : notnull, ThunderboltRegistration, new()
    {
        TRegistration reg = new();
        reg.GeneratedRegistration(this);
        reg.Register(this);
    }

    public IThunderboltScope CreateScope()
        => new ThunderboltScope(this);

    public T? Get<T>() where T : notnull
    {
        return ThunderboltServiceRegistry<T>.RegisteredLifetime switch
        {
            ThunderboltServiceLifetime.Transient => ThunderboltServiceRegistry<T>.Factory(this, ThunderboltServiceRegistry<T>.RegisteredImplSelector, ThunderboltServiceRegistry<T>.RegisteredUserFactory),
            ThunderboltServiceLifetime.Scoped or ThunderboltServiceLifetime.Singleton => ThunderboltServiceRegistry<T>.GetSingleton(this),
            _ => typeof(T).IsEnumerable() ? (T)typeof(T).EmptyEnumerable() : default!
        };
    }

    public object? GetService(Type serviceType)
    {
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
            ThunderboltServiceLifetime.Scoped or ThunderboltServiceLifetime.Singleton => genAcc.singletonInstanceGetter(this),
            _ => serviceType.IsEnumerable() ? serviceType.EmptyEnumerable() : default
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

    public void AddTransientFactory<TService>(Func<TService> factory) where TService : notnull
        => ThunderboltServiceRegistry<TService>.Register(ThunderboltServiceLifetime.Transient, resolver => factory());
    public void AddScopedFactory<TService>(Func<TService> factory) where TService : notnull
        => ThunderboltServiceRegistry<TService>.Register(ThunderboltServiceLifetime.Scoped, resolver => factory());
    public void AddSingletonFactory<TService>(Func<TService> factory) where TService : notnull
        => ThunderboltServiceRegistry<TService>.Register(ThunderboltServiceLifetime.Singleton, resolver => factory());
    public void AddTransientFactory<TService>(Func<IThunderboltResolver, TService> factory) where TService : notnull
        => ThunderboltServiceRegistry<TService>.Register(ThunderboltServiceLifetime.Transient, factory);
    public void AddScopedFactory<TService>(Func<IThunderboltResolver, TService> factory) where TService : notnull
        => ThunderboltServiceRegistry<TService>.Register(ThunderboltServiceLifetime.Scoped, factory);
    public void AddSingletonFactory<TService>(Func<IThunderboltResolver, TService> factory) where TService : notnull
        => ThunderboltServiceRegistry<TService>.Register(ThunderboltServiceLifetime.Singleton, factory);

    public void AddTransient<TService>(Func<Type> implSelector) where TService : notnull
        => ThunderboltServiceRegistry<TService>.Register(ThunderboltServiceLifetime.Transient, implSelector);
    public void AddScoped<TService>(Func<Type> implSelector) where TService : notnull
        => ThunderboltServiceRegistry<TService>.Register(ThunderboltServiceLifetime.Scoped, implSelector);
    public void AddSingleton<TService>(Func<Type> implSelector) where TService : notnull
        => ThunderboltServiceRegistry<TService>.Register(ThunderboltServiceLifetime.Singleton, implSelector);
    #endregion

    public void Dictate<T>(Func<IThunderboltResolver, Func<Type>?, Func<IThunderboltResolver, T>?, T> factory) where T : notnull
        => ThunderboltServiceRegistry<T>.Dictate(factory);
    public void Dictate<T>(Func<IThunderboltResolver, Func<Type>?, Func<IThunderboltResolver, T>?, T> factory, ThunderboltServiceLifetime serviceLifetime, Func<Type>? implSelector, Func<IThunderboltResolver, T>? userFactory) where T : notnull
        => ThunderboltServiceRegistry<T>.Dictate(factory, serviceLifetime, implSelector, userFactory);

    public void Dictate(in Type serviceType, in Func<IThunderboltResolver, Func<Type>?, Func<IThunderboltResolver, object>?, object> factory)
    {
        EnsureReflectionContextInitialized();
        var dictate = dictateDefinition!.MakeGenericMethod(serviceType);
        var createDictateFunc = createDictateFuncDefinition!.MakeGenericMethod(serviceType);
        dictate.Invoke(this, new object?[] { createDictateFunc.Invoke(null, new object?[] { factory }) });
    }
    public void Dictate(in Type serviceType, in Func<IThunderboltResolver, Func<Type>?, Func<IThunderboltResolver, object>?, object> factory, ThunderboltServiceLifetime serviceLifetime, Func<Type>? implSelector, Func<IThunderboltResolver, object>? userFactory)
    {
        EnsureReflectionContextInitialized();
        var dictate = fullDictateDefinition!.MakeGenericMethod(serviceType);
        var createDictateFunc = createDictateFuncDefinition!.MakeGenericMethod(serviceType).Invoke(null, new object[] { factory });
        var createUserFactoryFunc = userFactory is null ? null : createUserFactoryFuncDefinition!.MakeGenericMethod(serviceType).Invoke(null, new object[] { userFactory! });
        dictate.Invoke(this, new object?[] { createDictateFunc, serviceLifetime, implSelector!, createUserFactoryFunc! });
    }

    #region Microsoft.Extensions.DependencyInjection support
    private static bool isReflectionContextInitialized;

    private static System.Reflection.MethodInfo? dictateDefinition;
    private static System.Reflection.MethodInfo? fullDictateDefinition;
    private static System.Reflection.MethodInfo? createDictateFuncDefinition;
    private static System.Reflection.MethodInfo? createUserFactoryFuncDefinition;

    private static System.Reflection.MethodInfo? addTransientDefinition;
    private static System.Reflection.MethodInfo? addScopedDefinition;
    private static System.Reflection.MethodInfo? addSingletonDefinition;

    private static System.Reflection.MethodInfo? addTransientFactoryDefinition;
    private static System.Reflection.MethodInfo? addScopedFactoryDefinition;
    private static System.Reflection.MethodInfo? addSingletonFactoryDefinition;

    private static Func<IThunderboltResolver, T> CreateUserFactoryFunc<T>(Func<IThunderboltResolver, object> objFunc)
        => resolver => (T)objFunc(resolver);
    private static Func<IThunderboltResolver, Func<Type>?, Func<IThunderboltResolver, T>?, T> CreateDictateFunc<T>(Func<IThunderboltResolver, Func<Type>?, Func<IThunderboltResolver, object>?, object> objFunc)
        => (resolver, implSelector, userFactory)
            => (T)objFunc(resolver, implSelector, userFactory is null ? null : r => userFactory(r)!);

    private static void EnsureReflectionContextInitialized()
    {
        if (isReflectionContextInitialized)
            return;

        Type containerType = typeof(ThunderboltContainer);
        var containerMethods = containerType.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        foreach(System.Reflection.MethodInfo m in containerMethods)
        {
            switch (m)
            {
                case { Name: nameof(Dictate) } when m.GetGenericArguments().Length == 1 && m.GetParameters().Length == 1:
                    dictateDefinition = m;
                    break;
                case { Name: nameof(Dictate) } when m.GetGenericArguments().Length == 1 && m.GetParameters().Length == 4:
                    fullDictateDefinition = m;
                    break;
                case { Name: nameof(CreateDictateFunc) } when m.GetGenericArguments().Length == 1:
                    createDictateFuncDefinition = m;
                    break;
                case { Name: nameof(CreateUserFactoryFunc) } when m.GetGenericArguments().Length == 1:
                    createUserFactoryFuncDefinition = m;
                    break;

                case { Name: nameof(AddTransient) } when m.GetParameters().Length == 0 && m.GetGenericArguments().Length == 1:
                    addTransientDefinition = m;
                    break;
                case { Name: nameof(AddScoped) } when m.GetParameters().Length == 0 && m.GetGenericArguments().Length == 1:
                    addScopedDefinition = m;
                    break;
                case { Name: nameof(AddSingleton) } when m.GetParameters().Length == 0 && m.GetGenericArguments().Length == 1:
                    addSingletonDefinition = m;
                    break;

                case { Name: nameof(AddTransientFactory) } when m.GetParameters().First().ParameterType.GetGenericArguments().Length == 2:
                    addTransientFactoryDefinition = m;
                    break;
                case { Name: nameof(AddScopedFactory) } when m.GetParameters().First().ParameterType.GetGenericArguments().Length == 2:
                    addScopedFactoryDefinition = m;
                    break;
                case { Name: nameof(AddSingletonFactory) } when m.GetParameters().First().ParameterType.GetGenericArguments().Length == 2:
                    addSingletonFactoryDefinition = m;
                    break;
            }
        }

        isReflectionContextInitialized = true;
    }

    internal static void InternalDictate(in Type serviceType, in Func<IThunderboltResolver, Func<Type>?, Func<IThunderboltResolver, object>?, object> factory)
    {
        EnsureReflectionContextInitialized();
        var dictate = dictateDefinition!.MakeGenericMethod(serviceType);
        var createDictateFunc = createDictateFuncDefinition!.MakeGenericMethod(serviceType);
        dictate.Invoke(ThunderboltActivator.Container, new object?[] { createDictateFunc.Invoke(null, new object?[] { factory }) });
    }

    #region IThunderboltReflectionRegistrar
    public void AddTransientReflection(in Type serviceType)
    {
        EnsureReflectionContextInitialized();
        addTransientDefinition!.MakeGenericMethod(serviceType).Invoke(this, null);
    }
    public void AddScopedReflection(in Type serviceType)
    {
        EnsureReflectionContextInitialized();
        addScopedDefinition!.MakeGenericMethod(serviceType).Invoke(this, null);
    }
    public void AddSingletonReflection(in Type serviceType)
    {
        EnsureReflectionContextInitialized();
        addSingletonDefinition!.MakeGenericMethod(serviceType).Invoke(this, null);
    }

    public void AddTransientFactoryReflection(in Type serviceType, in Func<IThunderboltResolver, object> factory)
    {
        EnsureReflectionContextInitialized();
        var addTransientFactory = addTransientFactoryDefinition!.MakeGenericMethod(serviceType);
        var createUserFactoryFunc = createUserFactoryFuncDefinition!.MakeGenericMethod(serviceType);
        addTransientFactory.Invoke(this, new object?[] { createUserFactoryFunc.Invoke(null, new object?[] { factory }) });
    }
    public void AddScopedFactoryReflection(in Type serviceType, in Func<IThunderboltResolver, object> factory)
    {
        EnsureReflectionContextInitialized();
        var addScopedFactory = addScopedFactoryDefinition!.MakeGenericMethod(serviceType);
        var createUserFactoryFunc = createUserFactoryFuncDefinition!.MakeGenericMethod(serviceType);
        addScopedFactory.Invoke(this, new object?[] { createUserFactoryFunc.Invoke(null, new object?[] { factory }) });
    }
    public void AddSingletonFactoryReflection(in Type serviceType, in Func<IThunderboltResolver, object> factory)
    {
        EnsureReflectionContextInitialized();
        var addSingletonFactory = addSingletonFactoryDefinition!.MakeGenericMethod(serviceType);
        var createUserFactoryFunc = createUserFactoryFuncDefinition!.MakeGenericMethod(serviceType);
        addSingletonFactory.Invoke(this, new object?[] { createUserFactoryFunc.Invoke(null, new object?[] { factory }) });
    }
    #endregion
    #endregion
}
