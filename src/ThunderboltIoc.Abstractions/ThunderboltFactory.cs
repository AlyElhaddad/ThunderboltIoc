namespace ThunderboltIoc;

internal static partial class ThunderboltFactory
{
    private class FactoriesDictionary : Dictionary<Type, Func<IThunderboltResolver, object>>
    {
        public new void Add(Type key, Func<IThunderboltResolver, object> value)
        {
            if (!ContainsKey(key))
                base.Add(key, value);
        }
    }

    private static readonly FactoriesDictionary factories;
    static ThunderboltFactory()
    {
        factories = new();
        AddStaticFactories();
        AddFactories();
    }
    static partial void AddStaticFactories();
    static partial void AddFactories();
    public static object Create(IThunderboltResolver resolver, Type type) => factories[type](resolver);
}
