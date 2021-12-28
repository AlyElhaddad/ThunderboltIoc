namespace ThunderboltIoc;

public interface IThunderboltContainer : IThunderboltResolver
{
    IThunderboltScope CreateScope();
}
