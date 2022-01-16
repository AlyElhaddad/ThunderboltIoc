using System;

namespace benchmarks
{
    public abstract class FrameworkBenchmarks
    {
        public abstract void Startup();

        public abstract void Runtime();

        private protected string Dump(object obj)
        {
            //Console.WriteLine(obj);
            return obj.ToString();
        }
    }
}
