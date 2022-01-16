using benchmarks.Models;

using Grace.DependencyInjection;
using Grace.DependencyInjection.Lifestyle;

namespace benchmarks
{
    public sealed class GraceBenchmarks : FrameworkBenchmarks
    {
        private static DependencyInjectionContainer GetContainer()
        {
            var container = new DependencyInjectionContainer();

            container.Configure(m =>
            {
                m.Export<SingleService>().As<SingleService>().UsingLifestyle(new SingletonLifestyle());
                m.Export<FooService>().As<FooService>();
                m.Export<BarService>().As<BarService>();
                m.Export<BazService>().As<IBazService>();
            });

            return container;
        }

        private readonly DependencyInjectionContainer container;
        public GraceBenchmarks()
        {
            container = GetContainer();
        }

        public override void Startup()
        {
            Dump(GetContainer());
        }

        public override void Runtime()
        {
            Dump(container.Locate<SingleService>());
            Dump(container.Locate<FooService>());
            Dump(container.Locate<BarService>());
            Dump(container.Locate<IBazService>());
        }
    }
}
