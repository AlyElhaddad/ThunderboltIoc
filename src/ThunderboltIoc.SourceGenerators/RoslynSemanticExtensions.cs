using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using System.Text;

namespace ThunderboltIoc.SourceGenerators;

internal static class RoslynSemanticExtensions
{
    private static TResult MethodCache<T, TResult>(T input, Func<T, TResult> method, IDictionary<T, TResult> cache)
    {
        if (cache.ContainsKey(input))
            return cache[input];
        TResult result = method(input);
        cache.Add(input, result);
        return result;
    }

    public static string GetFullNamespaceName(this INamespaceSymbol namespaceSymbol)
    {
        StringBuilder nsBuilder = new(namespaceSymbol.Name);
        while (namespaceSymbol.ContainingNamespace != null)
        {
            namespaceSymbol = namespaceSymbol.ContainingNamespace;
            nsBuilder.Insert(0, namespaceSymbol.IsGlobalNamespace ? Consts.global : $"{namespaceSymbol.Name}.");
        }
        return nsBuilder.ToString();
    }

    private static readonly Dictionary<ITypeSymbol, string> typeFullNamesCache = new(SymbolEqualityComparer.Default);
    public static string GetFullyQualifiedName(this ITypeSymbol typeSymbol)
    {
        static string inv(ITypeSymbol typeSymbol) => $"{typeSymbol.ContainingNamespace.GetFullNamespaceName()}.{typeSymbol.Name}";
        return MethodCache(typeSymbol, inv, typeFullNamesCache);
    }

    private static readonly Dictionary<INamedTypeSymbol, string> namedTypeFullNamesCache = new(SymbolEqualityComparer.Default);
    public static string GetFullyQualifiedName(this INamedTypeSymbol namedTypeSymbol)
    {
        static string inv(INamedTypeSymbol namedTypeSymbol) => $"{namedTypeSymbol.ContainingNamespace.GetFullNamespaceName()}.{namedTypeSymbol.Name}{(namedTypeSymbol.IsGenericType ? $"<{string.Join(", ", namedTypeSymbol.TypeArguments.Select(p => p.Name))}>" : "")}";
        return MethodCache(namedTypeSymbol, inv, namedTypeFullNamesCache);
    }

    private static readonly Dictionary<INamespaceSymbol, IEnumerable<INamedTypeSymbol>> getAllTypeMembersCache = new(SymbolEqualityComparer.Default);
    public static IEnumerable<INamedTypeSymbol> GetAllTypeMembers(this INamespaceSymbol namespaceSymbol)
    {
        static IEnumerable<INamedTypeSymbol> inv(INamespaceSymbol namespaceSymbol)
        {
            foreach (INamedTypeSymbol child in namespaceSymbol.GetTypeMembers())
            {
                yield return child;
                foreach (INamedTypeSymbol nestedChild in child.GetTypeMembers())
                    yield return nestedChild;
            }

            foreach (INamedTypeSymbol childNamespaceTypeSymbol in namespaceSymbol.GetNamespaceMembers().SelectMany(ns => ns.GetAllTypeMembers()))
                yield return childNamespaceTypeSymbol;
        }
        return MethodCache(namespaceSymbol, inv, getAllTypeMembersCache);
    }

    public static IEnumerable<INamedTypeSymbol> GetAllTypeMembers(this Compilation compilation) => compilation.GlobalNamespace.GetAllTypeMembers();
    public static IEnumerable<INamespaceSymbol> GetAllNamespaceMembers(this INamespaceSymbol namespaceSymbol)
    {
        foreach (INamespaceSymbol parentNamespaceSymbol in namespaceSymbol.GetNamespaceMembers())
        {
            yield return parentNamespaceSymbol;
            foreach (INamespaceSymbol childNamespaceSymbol in parentNamespaceSymbol.GetAllNamespaceMembers())
                yield return childNamespaceSymbol;
        }
    }
    public static IEnumerable<INamespaceSymbol> GetAllNamespaceMembers(this Compilation compilation)
    {
        return compilation.GlobalNamespace.GetAllNamespaceMembers();
    }
    public static IEnumerable<string> GetAllNamespaceNames(this Compilation compilation)
    {
        return compilation.GlobalNamespace.GetAllNamespaceMembers().Select(ns => ns.GetFullNamespaceName());
    }

    public static IEnumerable<string> GetAssemblyFullTypeNames(this Compilation compilation)
    {
        return compilation.GetAllTypeMembers().Select(t => t.GetFullyQualifiedName().RemovePrefix(Consts.global)).Where(t => compilation.Assembly.NamespaceNames.Any(ns => t.StartsWith(ns)) && compilation.Assembly.TypeNames.Any(tn => t.EndsWith(tn)));
    }

    public static IEnumerable<INamedTypeSymbol> GetAssemblyFullTypeMembers(this Compilation compilation)
    {
        return compilation.GetAllTypeMembers().Where(t =>
        {
            string typeName = t.GetFullyQualifiedName().RemovePrefix(Consts.global);
            return compilation.Assembly.NamespaceNames.Any(ns => typeName.StartsWith(ns))
                && compilation.Assembly.TypeNames.Any(tn => typeName.EndsWith(tn));
        });
    }

    public static bool HasParent(this ITypeSymbol typeSymbol, ITypeSymbol parentType)
    {
        string parentName = parentType.GetFullyQualifiedName();
        while (typeSymbol.BaseType is not null)
        {
            string baseName = typeSymbol.BaseType.GetFullyQualifiedName();
            if (baseName == parentName)
                return true;
            typeSymbol = typeSymbol.BaseType;
        }
        return false;
    }

    public static ITypeSymbol? GetTypeByFullName(this Compilation compilation, string fullName)
    {
        SemanticModel semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
        return semanticModel.GetSpeculativeTypeInfo(0, SyntaxFactory.ParseTypeName(fullName), SpeculativeBindingOption.BindAsTypeOrNamespace).Type;
    }

    public static IEnumerable<INamedTypeSymbol?> GetTypesByFullName(this Compilation compilation, params string[] fullNames)
    {
        SemanticModel semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
        return
            fullNames.Select(fullName => semanticModel.GetSpeculativeTypeInfo(0, SyntaxFactory.ParseTypeName(fullName), SpeculativeBindingOption.BindAsTypeOrNamespace).Type as INamedTypeSymbol);
    }
}
