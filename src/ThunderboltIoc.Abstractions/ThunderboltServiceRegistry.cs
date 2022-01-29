namespace ThunderboltIoc;

internal readonly struct GenericRegistryAccessor
{
    internal readonly Func<ThunderboltServiceLifetime> lifetimeGetter;
    internal readonly Func<IThunderboltResolver, object> factory;
    internal readonly IReadOnlyDictionary<int, object> scopesInstances;
    internal readonly Func<IThunderboltResolver, object> singletonInstanceGetter;
    internal readonly Func<int, object, object> instanceSetter;

    public GenericRegistryAccessor(
        in Func<ThunderboltServiceLifetime> lifetimeGetter,
        in Func<IThunderboltResolver, object> factory,
        in IReadOnlyDictionary<int, object> scopesInstances,
        in Func<IThunderboltResolver, object> singletonInstanceGetter,
        in Func<int, object, object> instanceSetter)
    {
        this.lifetimeGetter = lifetimeGetter;
        this.factory = factory;
        this.scopesInstances = scopesInstances;
        this.singletonInstanceGetter = singletonInstanceGetter;
        this.instanceSetter = instanceSetter;
    }
}

internal static class ThunderboltServiceRegistry
{
    internal static readonly Dictionary<int, List<Action>> scopeClearanceActions = new();
    internal static readonly Dictionary<Type, GenericRegistryAccessor> generic = new(capacity: 3); // this is first accessed via the container's ctor, into which we later add two more registrations, making it a total of 3 (container, scope, resolver)
}
internal static class ThunderboltServiceRegistry<T>
    where T : notnull
{
    private static T? singletonInstance;
    internal static readonly Dictionary<int, T> scopesInstances = new();

    static ThunderboltServiceRegistry()
    {
        ThunderboltServiceRegistry.generic[typeof(T)] = new GenericRegistryAccessor(
            () => RegisteredLifetime,
            resolver => Factory(resolver, RegisteredImplSelector, RegisteredUserFactory),
            new DictionaryTypeAdapter<int, T, object>(scopesInstances),
            resolver => GetSingleton(resolver),
            (scopeId, instance) => instance is T genericInstance ? SetInstance(scopeId, genericInstance) : throw new ArgumentException($"The type of the specified argument is not '{typeof(T)}'.", nameof(instance)));
    }

    private static Func<IThunderboltResolver, Func<Type>?, Func<T>?, T>? dictatedFactory;
    internal static Func<IThunderboltResolver, Func<Type>?, Func<T>?, T> Factory
        => dictatedFactory ?? throw new InvalidOperationException($"Unable to locate a registered implementation for a service of type '{typeof(T)}'.");

    internal static ThunderboltServiceLifetime RegisteredLifetime;
    internal static Func<Type>? RegisteredImplSelector;
    internal static Func<T>? RegisteredUserFactory;

    internal static void Dictate(in Func<IThunderboltResolver, Func<Type>?, Func<T>?, T> factory)
    {
        dictatedFactory = factory;
    }

    internal static void Register(in ThunderboltServiceLifetime serviceLifetime)
    {
        RegisteredLifetime = serviceLifetime;
    }
    internal static void Register(in ThunderboltServiceLifetime serviceLifetime, in Func<T> userFactory)
    {
        RegisteredLifetime = serviceLifetime;
        RegisteredUserFactory = userFactory;
    }
    internal static void Register(in ThunderboltServiceLifetime serviceLifetime, in Func<Type> implSelector)
    {
        RegisteredLifetime = serviceLifetime;
        RegisteredImplSelector = implSelector;
    }

    #region scope management
    internal static T GetSingleton(in IThunderboltResolver resolver)
        => singletonInstance ??= Factory(resolver, RegisteredImplSelector, RegisteredUserFactory);

    internal static T SetInstance(int scopeId, T instance)
    {
        void removeAction() => scopesInstances.Remove(scopeId);
        void disposeAction()
        {
            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER || NET5_0_OR_GREATER
            else if (instance is IAsyncDisposable asyncDisposable)
            {
                asyncDisposable.DisposeAsync();
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
}
