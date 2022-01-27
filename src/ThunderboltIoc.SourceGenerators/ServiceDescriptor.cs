using Microsoft.CodeAnalysis;

namespace ThunderboltIoc.SourceGenerators;

internal readonly struct ServiceDescriptor
{
    internal ServiceDescriptor(int? lifetime, INamedTypeSymbol serviceSymbol, INamedTypeSymbol? implSymbol, IEnumerable<INamedTypeSymbol>? implSelectorSymbols, bool hasFactory, bool registeredByAttribute)
    {
        Lifetime = lifetime;
        ServiceSymbol = serviceSymbol;
        ImplSymbol = implSymbol;
        ImplSelectorSymbols = implSelectorSymbols;
        HasFactory = hasFactory;
        RegisteredByAttribute = registeredByAttribute;
    }

    public int? Lifetime { get; }
    public INamedTypeSymbol ServiceSymbol { get; }
    public INamedTypeSymbol? ImplSymbol { get; }
    public IEnumerable<INamedTypeSymbol>? ImplSelectorSymbols { get; }
    public bool HasFactory { get; }
    public bool RegisteredByAttribute { get; }
}
