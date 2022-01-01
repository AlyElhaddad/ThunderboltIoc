namespace ThunderboltIoc;

internal class ThunderboltFactory : IThunderboltFactoryDictator
{
    private readonly Dictionary<Type, Func<IThunderboltResolver, object>> serviceFactories;
    private ThunderboltFactory()
    {
        serviceFactories = new();
    }

    private static ThunderboltFactory? instance;
    internal static ThunderboltFactory Instance => instance ??= new();

    void IThunderboltFactoryDictator.Dictate(Type serviceType, Func<IThunderboltResolver, object> serviceFactory)
    {
        if (!serviceFactories.ContainsKey(serviceType))
            serviceFactories.Add(serviceType, serviceFactory);
    }

    internal object Create(IThunderboltResolver resolver, Type type) => serviceFactories[type](resolver);
}
