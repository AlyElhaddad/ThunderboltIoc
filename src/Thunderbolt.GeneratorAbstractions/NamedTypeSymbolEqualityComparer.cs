using Microsoft.CodeAnalysis;

namespace Thunderbolt.GeneratorAbstractions;

internal class NamedTypeSmbolEqualityComparer : IEqualityComparer<INamedTypeSymbol>
{
    private NamedTypeSmbolEqualityComparer() { }
    public static NamedTypeSmbolEqualityComparer Default { get; } = new NamedTypeSmbolEqualityComparer();

    public bool Equals(INamedTypeSymbol x, INamedTypeSymbol y)
    {
        return x?.GetFullyQualifiedName() == y?.GetFullyQualifiedName();
    }

    public int GetHashCode(INamedTypeSymbol obj)
    {
        return obj?.GetFullyQualifiedName()?.GetHashCode() ?? 0;
    }
}
