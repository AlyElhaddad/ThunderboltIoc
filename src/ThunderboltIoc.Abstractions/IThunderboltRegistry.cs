namespace ThunderboltIoc;

internal interface IThunderboltRegistry
{
    IReadOnlyDictionary<Type, ThunderboltRegister> Registers { get; }
}
