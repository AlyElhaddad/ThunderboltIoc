using System.Threading;

namespace benchmarks.Models
{
    public abstract class TransientServiceBase
    {
        private readonly SingleService singleService;

        protected TransientServiceBase(SingleService singleService)
        {
            Id = Interlocked.Increment(ref StaticServiceId.globalServiceId);
            this.singleService = singleService;
        }

        public int Id { get; }
        public SingleService SingleService => singleService;

        public override string ToString()
        {
            return $"{GetType()} - {Id}";
        }
    }
}
