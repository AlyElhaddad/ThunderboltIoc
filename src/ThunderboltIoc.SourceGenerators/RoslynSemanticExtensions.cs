using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using System.Text;

namespace ThunderboltIoc.SourceGenerators;

internal static class RoslynSemanticExtensions
{
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

    public static string GetFullyQualifiedName(this ITypeSymbol typeSymbol)
    {
        return $"{typeSymbol.ContainingNamespace.GetFullNamespaceName()}.{typeSymbol.Name}";
    }

    public static string GetFullyQualifiedName(this INamedTypeSymbol namedTypeSymbol)
    {
        return $"{namedTypeSymbol.ContainingNamespace.GetFullNamespaceName()}.{namedTypeSymbol.Name}{(namedTypeSymbol.IsGenericType ? $"<{string.Join(", ", namedTypeSymbol.TypeArguments.Select(p => p.Name))}>" : "")}";
    }

    public static IEnumerable<INamedTypeSymbol> GetAllTypeMembers(this INamespaceSymbol namespaceSymbol)
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

    public static IEnumerable<INamedTypeSymbol> GetAllTypeMembers(this Compilation compilation) => compilation.GlobalNamespace.GetAllTypeMembers();

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

    public static INamedTypeSymbol? GetTypeByFullName(this Compilation compilation, string fullName)
    {
        SemanticModel semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
        return semanticModel.GetSpeculativeTypeInfo(0, SyntaxFactory.ParseTypeName(fullName), SpeculativeBindingOption.BindAsTypeOrNamespace).Type as INamedTypeSymbol;
    }
    public static INamedTypeSymbol? GetTypeByFullName(this SemanticModel semanticModel, string fullName)
    {
        return semanticModel.GetSpeculativeTypeInfo(0, SyntaxFactory.ParseTypeName(fullName), SpeculativeBindingOption.BindAsTypeOrNamespace).Type as INamedTypeSymbol;
    }

    public static string GetMethodFullName(this IMethodSymbol method)
    {
        return $"{method.ContainingType.GetFullyQualifiedName()}.{method.Name}";
    }

    public static bool AreDefinitionsEqual(this IMethodSymbol method, IMethodSymbol equalMethod)
    {
        method = method.OriginalDefinition;
        equalMethod = equalMethod.OriginalDefinition;
        return ReferenceEquals(method, equalMethod)
            ||
            (method.GetMethodFullName() == equalMethod.GetMethodFullName()
            && method.TypeParameters.Length == equalMethod.TypeParameters.Length
            && method.Parameters.Length == equalMethod.Parameters.Length
            && method.Parameters.IndexTupleJoin(equalMethod.Parameters).All(item => item.left.Type.GetFullyQualifiedName() == item.right.Type.GetFullyQualifiedName()));
    }
}
