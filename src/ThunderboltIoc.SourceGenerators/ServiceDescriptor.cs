using Microsoft.CodeAnalysis;

namespace ThunderboltIoc.SourceGenerators;

internal readonly struct ServiceDescriptor : IEquatable<ServiceDescriptor>
{
    internal ServiceDescriptor(
        int? lifetime,
        INamedTypeSymbol serviceSymbol,
        INamedTypeSymbol? implSymbol,
        IEnumerable<INamedTypeSymbol>? implSelectorSymbols,
        bool hasFactory,
        bool registeredByAttribute)
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

    public IEnumerable<INamedTypeSymbol> GetPossibleImplementations(IEnumerable<ServiceDescriptor> allServices)
    {
        if (ImplSymbol is not null)
        {
            if (!ImplSymbol.IsAbstract)
                yield return ImplSymbol;
            yield break;
        }

        if (ImplSelectorSymbols is not null)
        {
            foreach (var impl in ImplSelectorSymbols)
            {
                if (allServices.FirstOrDefault(service => service.ServiceSymbol.GetFullyQualifiedName() == impl.GetFullyQualifiedName()) is ServiceDescriptor serviceDescriptor && serviceDescriptor.ServiceSymbol is INamedTypeSymbol serviceSymbol)
                {
                    foreach (INamedTypeSymbol implImpl in serviceDescriptor.GetPossibleImplementations(allServices))
                        yield return implImpl;
                }
                else if (!impl.IsAbstract)
                {
                    yield return impl;
                }
            }
            yield break;
        }

        if (!ServiceSymbol.IsAbstract)
            yield return ServiceSymbol;
    }

    #region Equality (via ServiceSymbol)
    public override bool Equals(object obj)
    {
        return obj is ServiceDescriptor other && Equals(other);
    }

    public bool Equals(ServiceDescriptor other)
    {
        return ServiceSymbol?.GetFullyQualifiedName() == other.ServiceSymbol?.GetFullyQualifiedName();
    }

    public override int GetHashCode()
    {
#pragma warning disable RS1024 // Symbols should be compared for equality
        return ServiceSymbol.GetHashCode();
#pragma warning restore RS1024 // Symbols should be compared for equality
    }
    #endregion
}
