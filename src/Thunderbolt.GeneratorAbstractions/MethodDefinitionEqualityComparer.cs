using Microsoft.CodeAnalysis;

namespace Thunderbolt.GeneratorAbstractions;

internal class MethodDefinitionEqualityComparer : IEqualityComparer<IMethodSymbol>
{
    private MethodDefinitionEqualityComparer() { }
    public static MethodDefinitionEqualityComparer Default = new();
    public bool Equals(IMethodSymbol x, IMethodSymbol y)
    {
        return x.AreDefinitionsEqual(y);
    }

    public int GetHashCode(IMethodSymbol obj)
    {
        return obj.GetMethodFullName().GetHashCode();
    }
}
