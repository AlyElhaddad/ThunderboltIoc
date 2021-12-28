namespace ThunderboltIoc;

public interface IThunderboltScope : IThunderboltResolver, IDisposable
{
    Guid Id { get; }
}
