using System;

using ThunderboltIoc;

using Xamarin.Forms.Xaml;

namespace ThunderForms.MarkupExtensions
{
    //Generic version, recommended because of a tiny performance advantage, but both the generic and the non-generic version should work
    public class ThunderboltResolveExtension<T> : IMarkupExtension<T>, IMarkupExtension
    {
        public ThunderboltResolveExtension()
        {
        }

        public T ProvideValue(IServiceProvider serviceProvider)
        {
            return ThunderboltActivator.Get<T>();
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        {
            return ThunderboltActivator.Get<T>();
        }
    }
    //Non-generic version
    public class ThunderboltResolveExtension : IMarkupExtension
    {
        public ThunderboltResolveExtension()
        {
        }
        public ThunderboltResolveExtension(Type type)
        {
            Type = type;
        }

        public Type Type { get; set; }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            return Type is null ? null : ThunderboltActivator.Container.GetService(Type);
        }
    }
}
