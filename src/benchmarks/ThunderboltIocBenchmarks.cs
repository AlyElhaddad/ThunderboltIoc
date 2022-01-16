using benchmarks.Models;

using ThunderboltIoc;

namespace benchmarks
{
    internal partial class ThunderboltServiceRegistration : ThunderboltRegistration
    {
        protected override void Register(IThunderboltRegistrar reg)
        {
            reg.AddSingleton<SingleService>();
            reg.AddTransient<FooService>();
            reg.AddTransient<BarService>();
            reg.AddTransient<IBazService, BazService>();
        }
    }

    public sealed class ThunderboltIocBenchmarks : FrameworkBenchmarks
    {
        internal static IThunderboltContainer container;
        
        internal static void Attach()
        {
            ThunderboltActivator.Attach<ThunderboltServiceRegistration>();
        }

        public override void Startup()
        {
            Attach();
        }

        public override void Runtime()
        {
            Dump(container.Get<SingleService>());
            Dump(container.Get<FooService>());
            Dump(container.Get<BarService>());
            Dump(container.Get<IBazService>());
        }
    }
}
