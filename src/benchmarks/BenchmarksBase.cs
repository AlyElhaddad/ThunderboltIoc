namespace benchmarks
{
    public abstract class BenchmarksBase
    {
        private protected readonly ThunderboltIocBenchmarks thunderboltBenchmarks;
        private protected readonly MicrosoftDIBenchmarks microsoftBenchmarks;
        private protected readonly AutofacBenchmarks autofacBenchmarks;
        private protected readonly GraceBenchmarks graceBenchmarks;

        public BenchmarksBase()
        {
            thunderboltBenchmarks = new ThunderboltIocBenchmarks();
            microsoftBenchmarks = new MicrosoftDIBenchmarks();
            autofacBenchmarks = new AutofacBenchmarks();
            graceBenchmarks = new GraceBenchmarks();
        }
    }
}
