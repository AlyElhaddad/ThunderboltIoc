using BenchmarkDotNet.Running;

using System;

namespace benchmarks
{
    internal class Program
    {

        static void Main(string[] args)
        {
            try
            {
                BenchmarkRunner.Run<StartupBenchmarks>();
                BenchmarkRunner.Run<RuntimeBenchmarks>();

                //to make sure everything works:

                //Console.WriteLine("Thunderbolt");
                //ThunderboltIocBenchmarks thunderboltIocBenchmarks = new ThunderboltIocBenchmarks();
                //thunderboltIocBenchmarks.Startup();
                //ThunderboltIocBenchmarks.container = ThunderboltActivator.Container;
                //thunderboltIocBenchmarks.Runtime();

                //StaticServiceId.globalServiceId = 0;

                //Console.WriteLine("Microsoft");
                //MicrosoftDIBenchmarks microsoftDIBenchmarks = new MicrosoftDIBenchmarks();
                //microsoftDIBenchmarks.Startup();
                //microsoftDIBenchmarks.Runtime();

                //StaticServiceId.globalServiceId = 0;

                //Console.WriteLine("Grace");
                //GraceBenchmarks graceBenchmarks = new GraceBenchmarks();
                //graceBenchmarks.Startup();
                //graceBenchmarks.Runtime();

                Console.WriteLine("Benchmarks complete!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("The following error occurred while running the benchmarks.");
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine();
            Console.WriteLine("Press Esc to exit...");
            while (Console.ReadKey().Key != ConsoleKey.Escape) ;
        }
    }
}
