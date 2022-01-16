using Autofac;

using benchmarks.Models;

namespace benchmarks
{
    public sealed class AutofacBenchmarks : FrameworkBenchmarks
    {
        private static IContainer GetContainer()
        {
            ContainerBuilder builder = new ContainerBuilder();

            builder.RegisterType<SingleService>().SingleInstance();
            builder.RegisterType<FooService>().InstancePerDependency();
            builder.RegisterType<BarService>().InstancePerDependency();
            builder.RegisterType<BazService>().InstancePerDependency().As<IBazService>();

            return builder.Build();
        }

        private readonly IContainer container;
        public AutofacBenchmarks()
        {
            container = GetContainer();
        }

        public override void Startup()
        {
            Dump(GetContainer());
        }
        public override void Runtime()
        {
            Dump(container.Resolve<SingleService>());
            Dump(container.Resolve<FooService>());
            Dump(container.Resolve<BarService>());
            Dump(container.Resolve<IBazService>());
        }
    }
}
