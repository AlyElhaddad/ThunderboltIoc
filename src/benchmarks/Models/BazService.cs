namespace benchmarks.Models
{
    public class BazService : TransientServiceBase, IBazService
    {
        private readonly FooService fooService;
        private readonly BarService barService;

        public BazService(SingleService singleService, FooService fooService, BarService barService) : base(singleService)
        {
            this.fooService = fooService;
            this.barService = barService;
        }

        public FooService FooService => fooService;
        public BarService BarService => barService;
    }
}
