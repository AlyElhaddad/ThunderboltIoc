using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;

using ThunderboltIoc;

namespace benchmarks
{
    [SimpleJob(RunStrategy.Throughput)]
    [SimpleJob(RunStrategy.Monitoring)]
    [SimpleJob(RunStrategy.ColdStart)]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [MemoryDiagnoser]
    [MeanColumn, MedianColumn]
    public class RuntimeBenchmarks : BenchmarksBase
    {
#pragma warning disable CA1822 // Mark members as static
        [GlobalSetup]
        public void GlobalSetup()
        {
            ThunderboltIocBenchmarks.Attach();
            ThunderboltIocBenchmarks.container = ThunderboltActivator.Container;
        }
#pragma warning restore CA1822 // Mark members as static

        [Benchmark(Baseline = true)]
        public void ThunderboltIoc_Runtime()
        {
            thunderboltBenchmarks.Runtime();
        }

        [Benchmark]
        public void MicrosoftDI_Runtime()
        {
            microsoftBenchmarks.Runtime();
        }

        [Benchmark]
        public void Autofac_Runtime()
        {
            autofacBenchmarks.Runtime();
        }

        [Benchmark]
        public void Grace_Runtime()
        {
            graceBenchmarks.Runtime();
        }
    }
}
