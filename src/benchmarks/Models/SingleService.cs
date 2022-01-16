using System.Threading;

namespace benchmarks.Models
{
    public class SingleService
    {
        public SingleService()
        {
            Id = Interlocked.Increment(ref StaticServiceId.globalServiceId);
        }

        public int Id { get; }

        public override string ToString()
        {
            return $"SingleService - {Id}";
        }
    }
}
