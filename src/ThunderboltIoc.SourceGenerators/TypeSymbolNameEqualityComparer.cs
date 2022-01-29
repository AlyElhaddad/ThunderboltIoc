using Microsoft.CodeAnalysis;

namespace ThunderboltIoc.SourceGenerators
{
    internal class TypeSymbolNameEqualityComparer : IEqualityComparer<ITypeSymbol>
    {
        private TypeSymbolNameEqualityComparer() { }
        private static readonly TypeSymbolNameEqualityComparer _default = new();
        public static TypeSymbolNameEqualityComparer Default => _default;


        public bool Equals(ITypeSymbol x, ITypeSymbol y)
        {
            return x?.GetFullyQualifiedName() == y?.GetFullyQualifiedName();
        }

        public int GetHashCode(ITypeSymbol obj)
        {
            return obj?.GetFullyQualifiedName()?.GetHashCode() ?? 0;
        }
    }
}
