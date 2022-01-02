namespace ThunderboltIoc;

public interface IThunderboltContainer : IThunderboltResolver
{
    /// <summary>
    /// Creates and returns a new <see cref="IThunderboltScope"/>.
    /// </summary>
    IThunderboltScope CreateScope();
}
