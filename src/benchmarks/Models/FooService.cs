namespace benchmarks.Models
{
    public class FooService : TransientServiceBase
    {
        public FooService(SingleService singleService) : base(singleService)
        {
        }
    }
}
