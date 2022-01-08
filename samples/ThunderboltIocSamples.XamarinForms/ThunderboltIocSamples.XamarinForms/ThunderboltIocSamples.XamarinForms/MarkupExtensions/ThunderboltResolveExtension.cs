using System;

using ThunderboltIoc;

using Xamarin.Forms.Xaml;

namespace ThunderboltIocSamples.XamarinForms.MarkupExtensions
{
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
