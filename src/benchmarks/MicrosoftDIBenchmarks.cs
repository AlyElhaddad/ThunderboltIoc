using benchmarks.Models;

using Microsoft.Extensions.DependencyInjection;

using System;

namespace benchmarks
{
    public sealed class MicrosoftDIBenchmarks : FrameworkBenchmarks
    {
        private static IServiceProvider GetServiceProvider()
        {
            ServiceCollection services = new ServiceCollection();

            services.AddSingleton<SingleService>();
            services.AddTransient<FooService>();
            services.AddTransient<BarService>();
            services.AddTransient<IBazService, BazService>();

            return services.BuildServiceProvider();
        }


        private readonly IServiceProvider serviceProvider;
        public MicrosoftDIBenchmarks()
        {
            serviceProvider = GetServiceProvider();
        }

        public override void Startup()
        {
            Dump(GetServiceProvider());
        }

        public override void Runtime()
        {
            Dump(serviceProvider.GetService<SingleService>());
            Dump(serviceProvider.GetService<FooService>());
            Dump(serviceProvider.GetService<BarService>());
            Dump(serviceProvider.GetService<IBazService>());
        }
    }
}
