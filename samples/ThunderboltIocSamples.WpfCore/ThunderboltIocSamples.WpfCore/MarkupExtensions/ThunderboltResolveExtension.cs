using System;
using System.Windows.Markup;

using ThunderboltIoc;

namespace ThunderboltIocSamples.WpfCore.MarkupExtensions
{
    public class ThunderboltResolveExtension : MarkupExtension
    {
        public ThunderboltResolveExtension()
        {
        }
        public ThunderboltResolveExtension(Type type)
        {
            Type = type;
        }

        public Type Type { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Type is null ? null : ThunderboltActivator.Container.GetService(Type);
        }
    }
}
