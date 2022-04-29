using System.Linq;
using System.Reflection;

using Thunderbolt.GeneratorAbstractions;

namespace ThunderboltIoc;

internal readonly struct GenericRegistryAccessor
{
    internal readonly Func<ThunderboltServiceLifetime?> lifetimeGetter;
    internal readonly Func<IThunderboltResolver, object> factory;
    internal readonly IReadOnlyDictionary<int, object> scopesInstances;
    internal readonly Func<IThunderboltResolver, object> singletonInstanceGetter;
    internal readonly Func<int, object, object> instanceSetter;
    internal readonly Func<IReadOnlyList<HistoricalThunderboltServiceRegistry>> registryHistory;

    public GenericRegistryAccessor(
        in Func<ThunderboltServiceLifetime?> lifetimeGetter,
        in Func<IThunderboltResolver, object> factory,
        in IReadOnlyDictionary<int, object> scopesInstances,
        in Func<IThunderboltResolver, object> singletonInstanceGetter,
        in Func<int, object, object> instanceSetter,
        in Func<IReadOnlyList<HistoricalThunderboltServiceRegistry>> registryHistory)
    {
        this.lifetimeGetter = lifetimeGetter;
        this.factory = factory;
        this.scopesInstances = scopesInstances;
        this.singletonInstanceGetter = singletonInstanceGetter;
        this.instanceSetter = instanceSetter;
        this.registryHistory = registryHistory;
    }
}

internal record HistoricalThunderboltServiceRegistry
{
    internal readonly Func<IThunderboltResolver, Func<Type>?, Func<IThunderboltResolver, object>?, object> DictatedFactory;
    internal readonly ThunderboltServiceLifetime RegisteredLifetime;
    internal readonly Func<Type>? RegisteredImplSelector;
    internal readonly Func<IThunderboltResolver, object>? RegisteredUserFactory;

    public HistoricalThunderboltServiceRegistry(
        Func<IThunderboltResolver, Func<Type>?, Func<IThunderboltResolver, object>?, object> dictatedFactory,
        ThunderboltServiceLifetime registeredLifetime,
        Func<Type>? registeredImplSelector,
        Func<IThunderboltResolver, object>? registeredUserFactory)
    {
        DictatedFactory = dictatedFactory;
        RegisteredLifetime = registeredLifetime;
        RegisteredImplSelector = registeredImplSelector;
        RegisteredUserFactory = registeredUserFactory;
    }
}

internal static class ThunderboltServiceRegistry
{
    internal static long globalRecordId;
    internal static readonly Dictionary<int, List<Action>> scopeClearanceActions = new();
    internal static readonly Dictionary<Type, GenericRegistryAccessor> generic = new(capacity: 4); // this is first accessed via the container's ctor, into which we later add two more registrations, making it a total of 3 (container, scope, resolver, serviceProvider)

    internal static InvalidOperationException UnableToLocateException(Type t)
        => new($"Unable to locate a registered implementation for a service of type '{t}'.");

    private static IEnumerable<T> CreateEnumerable<T>(IEnumerable<object> enumerable)
        => enumerable.Cast<T>();
    private static MethodInfo? createEnumerableMethodDef;
    private static MethodInfo CreateEnumerableMethodDef
        => createEnumerableMethodDef ??= typeof(ThunderboltServiceRegistry).GetMethod(nameof(CreateEnumerable), BindingFlags.Static | BindingFlags.NonPublic).GetGenericMethodDefinition();

    internal static ThunderboltServiceLifetime? InitializeAndGetServiceLifetime(Type t)
    {
        if (t.IsGenericType)
        {
            Type typeDef = t.GetGenericTypeDefinition();
            if (ThunderboltRegistration.PrivateTypes.TryGetValue(typeDef.GetFullyQualifiedName(), out PrivateType genericDefinitionPrivateType)
                && genericDefinitionPrivateType.dictateFactory is not null)
            { //Open Generic Type
                Type[] typeArgs = t.GetGenericArguments();
                var dictate = genericDefinitionPrivateType.dictateFactory!(typeArgs);
                ThunderboltContainer.InternalDictate(t, dictate);
                return genericDefinitionPrivateType.registerLifetime;
            }
            else if (typeDef.IsEnumerable())
            { // IEnumerable<T>
                Type genArg = t.GetGenericArguments()[0];
                MethodInfo createEnumerableMethod = CreateEnumerableMethodDef.MakeGenericMethod(genArg);
                ThunderboltContainer.InternalDictate(t, (resolver, _, _) =>
                {
                    object enumerable
                        = generic
                        //take a copy of the dictionary because resolving may modify the dictionary and we may end up with InvalidOperationException
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                        .Where(kvp => genArg.IsAssignableFrom(kvp.Key))
                        .SelectMany(kvp => //resolver.GetService(kvp.Key)) //kvp.Value.factory(resolver))
                        {
                            var services = kvp.Value.factory(resolver).AsEnumerable();
                            var history = kvp.Value.registryHistory();
                            if (history.Any())
                            {
                                services = services.Concat(history.Select(record => record.DictatedFactory(resolver, record.RegisteredImplSelector, record.RegisteredUserFactory))).Reverse();
                            }
                            return services;
                            //return kvp.Value.lifetimeGetter() switch
                            //{
                            //    ThunderboltServiceLifetime.Transient => kvp.Value.factory(resolver),
                            //    ThunderboltServiceLifetime.Scoped when resolver is IThunderboltScope scope => new object(), //scoped instance accessor (implemented type)
                            //    ThunderboltServiceLifetime.Singleton or ThunderboltServiceLifetime.Scoped => new object(), //singleton instance accessor (implemented type)
                            //    _ => null
                            //};
                        })
                        .Where(value => value is not null);
                        //.ToArray();
                    return createEnumerableMethod.Invoke(null, new object[1] { enumerable });
                });
                return ThunderboltServiceLifetime.Transient; //open generic types are registered on the fly and therefore this shouldn't be singleton as there could always be new types
            }
        }
        return null;
    }
}

internal static class ThunderboltServiceRegistry<T>
    where T : notnull
{
    //private static long recordId;
    private static readonly object syncLock = new object();
    internal static readonly Dictionary<int, T> scopesInstances = new();
    private static bool initialized;
    private static T? singletonInstance;

    static ThunderboltServiceRegistry()
    {
        //recordId = Interlocked.Increment(ref ThunderboltServiceRegistry.globalRecordId);
        StaticInit();
    }

    private static void StaticInit()
    {
        ThunderboltServiceRegistry.generic[typeof(T)] = new GenericRegistryAccessor(
             () => RegisteredLifetime,
             resolver => Factory(resolver, GetImplSelector(), GetUserFactory()),
             new DictionaryTypeAdapter<int, T, object>(scopesInstances),
             resolver => GetSingleton(resolver),
             (scopeId, instance) => instance is T genericInstance ? SetInstance(scopeId, genericInstance) : throw new ArgumentException($"The type of the specified argument is not '{typeof(T)}'.", nameof(instance)),
             () => ThunderboltServiceRegistryHistory);
    }

    private static InvalidOperationException? unableToLocateException;
    private static InvalidOperationException UnableToLocateException
        => unableToLocateException ??= ThunderboltServiceRegistry.UnableToLocateException(typeof(T));

    private static Func<IThunderboltResolver, Func<Type>?, Func<IThunderboltResolver, T>?, T>? dictatedFactory;
    internal static Func<IThunderboltResolver, Func<Type>?, Func<IThunderboltResolver, T>?, T> Factory
        => dictatedFactory ?? throw UnableToLocateException;

    private static ThunderboltServiceLifetime? registeredLifetime;
    internal static ThunderboltServiceLifetime? RegisteredLifetime
    {
        get
        {
            lock (syncLock)
                return
                    initialized
                    ? registeredLifetime
                    : (registeredLifetime = ThunderboltServiceRegistry.InitializeAndGetServiceLifetime(typeof(T)));
        }
        set => registeredLifetime = value;
    }
    internal static Func<Type>? RegisteredImplSelector;
    internal static Func<IThunderboltResolver, T>? RegisteredUserFactory;

    private static Func<Type>? GetImplSelector() => RegisteredImplSelector;
    private static Func<IThunderboltResolver, T>? GetUserFactory() => RegisteredUserFactory;

    internal static void Dictate(in Func<IThunderboltResolver, Func<Type>?, Func<IThunderboltResolver, T>?, T> factory)
    {
        if (typeof(T).GetFullyQualifiedName().Contains("global::Microsoft.Extensions.Options.IConfigureOptions<global::Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>"))
        { }
        //if (typeof(T).GetFullyQualifiedName() == "global::Microsoft.AspNetCore.Hosting.Server.IServer")
        //{ }
        ArchiveIfNeedBe();
        dictatedFactory = factory;
    }
    internal static void Dictate(in Func<IThunderboltResolver, Func<Type>?, Func<IThunderboltResolver, T>?, T> factory, ThunderboltServiceLifetime serviceLifetime, Func<Type>? implSelector, Func<IThunderboltResolver, T>? userFactory)
    {
        if (typeof(T).GetFullyQualifiedName().Contains("global::Microsoft.Extensions.Options.IConfigureOptions<global::Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>"))
        { }
        ArchiveIfNeedBe();
        dictatedFactory = factory;

        initialized = true;
        RegisteredLifetime = serviceLifetime;
        RegisteredImplSelector = implSelector;
        RegisteredUserFactory = userFactory;
    }

    internal static void Register(in ThunderboltServiceLifetime serviceLifetime)
    {
        initialized = true;
        RegisteredLifetime = serviceLifetime;

        RegisteredImplSelector = default;
        RegisteredUserFactory = default;
    }
    internal static void Register(in ThunderboltServiceLifetime serviceLifetime, in Func<Type> implSelector)
    {
        initialized = true;
        RegisteredLifetime = serviceLifetime;
        RegisteredImplSelector = implSelector;

        RegisteredUserFactory = default;
    }
    internal static void Register(in ThunderboltServiceLifetime serviceLifetime, in Func<IThunderboltResolver, T> userFactory)
    {
        initialized = true;
        RegisteredLifetime = serviceLifetime;
        RegisteredUserFactory = userFactory;

        RegisteredImplSelector = default;
    }

    #region scope management
    internal static T GetSingleton(in IThunderboltResolver resolver)
        => singletonInstance ??= Factory(resolver, RegisteredImplSelector, RegisteredUserFactory);

    internal static T SetInstance(int scopeId, T instance)
    {
        void removeAction() => scopesInstances.Remove(scopeId);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER || NET5_0_OR_GREATER
        async
#endif
        void disposeAction()
        {
            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER || NET5_0_OR_GREATER
            else if (instance is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
#endif
        }

        if (ThunderboltServiceRegistry.scopeClearanceActions.TryGetValue(scopeId, out var actions) && actions is not null)
        {
            actions.Add(removeAction);
            actions.Add(disposeAction);
        }
        else
        {
            ThunderboltServiceRegistry.scopeClearanceActions[scopeId] = new List<Action>(capacity: 2)
            {
                removeAction,
                disposeAction
            };
        }
        return scopesInstances[scopeId] = instance;
    }
    #endregion

    #region Historical / IEnumerable<T> handling

    private static List<HistoricalThunderboltServiceRegistry>? thunderboltServiceRegistryHistory;
    private static List<HistoricalThunderboltServiceRegistry> ThunderboltServiceRegistryHistory => thunderboltServiceRegistryHistory ??= new List<HistoricalThunderboltServiceRegistry>();

    private static void ArchiveIfNeedBe()
    {
        if (dictatedFactory is null || !initialized)
            return;

        //recordId = Interlocked.Increment(ref ThunderboltServiceRegistry.globalRecordId);

        var copiedDictatedFactory = (Func<IThunderboltResolver, Func<Type>?, Func<IThunderboltResolver, T>?, T>)dictatedFactory!; //.Clone();
        var copiedImplSelector = (Func<Type>?)RegisteredImplSelector; //?.Clone();
        var copiedUserFactory = (Func<IThunderboltResolver, T>?)RegisteredUserFactory; //?.Clone();

        ThunderboltServiceRegistryHistory.Add(
            new HistoricalThunderboltServiceRegistry(
                (resolver, implSelector, userFactory) => copiedDictatedFactory(resolver, implSelector, userFactory is null ? default : (userFactoryResolver => (T)userFactory(userFactoryResolver))),
                registeredLifetime!.Value,
                copiedImplSelector,
                userFactoryResolver => copiedUserFactory!(userFactoryResolver)));

        dictatedFactory = null;
        initialized = false;
        registeredLifetime = null;
        RegisteredImplSelector = null;
        RegisteredUserFactory = null;

        StaticInit();
    }

    #endregion
}
