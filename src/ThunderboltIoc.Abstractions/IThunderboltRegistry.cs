namespace ThunderboltIoc;

public interface IThunderboltRegistry
{
    IReadOnlyDictionary<Type, ThunderboltRegister> Registers { get; }
}
