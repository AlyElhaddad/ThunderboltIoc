using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;

using System.Reflection;

using ThunderboltIoc;

namespace benchmarks
{
    [SimpleJob(RunStrategy.Throughput)]
    [SimpleJob(RunStrategy.Monitoring)]
    [SimpleJob(RunStrategy.ColdStart)]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [MemoryDiagnoser]
    [MeanColumn, MedianColumn]
    public class StartupBenchmarks : BenchmarksBase
    {
        private static readonly FieldInfo thunderboltContainer
            = typeof(ThunderboltActivator).GetField("container", BindingFlags.NonPublic | BindingFlags.Static);

#pragma warning disable CA1822 // Mark members as static
        [IterationSetup]
        public void IterationSetup()
        {
            thunderboltContainer.SetValue(null, null);
        }
        [GlobalCleanup]
        public void GlobalCleanup()
        {
            thunderboltContainer.SetValue(null, null);
        }
#pragma warning restore CA1822 // Mark members as static

        [Benchmark(Baseline = true)]
        public void ThunderboltIoc_Startup()
        {
            thunderboltBenchmarks.Startup();
        }

        [Benchmark]
        public void MicrosoftDI_Startup()
        {
            microsoftBenchmarks.Startup();
        }

        [Benchmark]
        public void Autofac_Startup()
        {
            autofacBenchmarks.Startup();
        }

        [Benchmark]
        public void Grace_Startup()
        {
            graceBenchmarks.Startup();
        }
    }
}
