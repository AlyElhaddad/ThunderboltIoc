namespace benchmarks.Models
{
    public class BarService : TransientServiceBase
    {
        private readonly FooService fooService;

        public BarService(SingleService singleService, FooService fooService) : base(singleService)
        {
            this.fooService = fooService;
        }

        public FooService FooService => fooService;
    }
}
