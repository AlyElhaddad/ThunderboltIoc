namespace ThunderboltIoc;

public interface IThunderboltResolver : IServiceProvider
{
    T Get<T>();
}
