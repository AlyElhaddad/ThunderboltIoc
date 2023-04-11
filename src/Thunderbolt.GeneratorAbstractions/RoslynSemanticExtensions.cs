using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using System.Text;

namespace Thunderbolt.GeneratorAbstractions;

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

    public static string? GetFullyQualifiedName(this ITypeSymbol? typeSymbol)
        => typeSymbol.GetFullyQualifiedName(false);
    private static string? GetFullyQualifiedName(this ITypeSymbol? typeSymbol, bool withoutGenerics)
    {
        if (typeSymbol is null)
            return null;
        else if (typeSymbol is ITypeParameterSymbol typeParamSymbol)
            return $"{typeParamSymbol.DeclaringType.GetFullyQualifiedName(true)}@{typeSymbol.Name}";

        return $"{(typeSymbol.ContainingType is INamedTypeSymbol containingType ? containingType.GetFullyQualifiedName() : typeSymbol.ContainingNamespace.GetFullNamespaceName())}.{typeSymbol.Name}{(!withoutGenerics && typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType ? $"<{string.Join(", ", (namedTypeSymbol.IsUnboundGenericType ? namedTypeSymbol.TypeParameters.Cast<ITypeSymbol>() : namedTypeSymbol.TypeArguments).Select(p => p.GetFullyQualifiedName()))}>" : "")}";
    }
    public static string? GetFullyQualifiedName(this INamedTypeSymbol? namedTypeSymbol)
        => namedTypeSymbol.GetFullyQualifiedName(false);
    private static string? GetFullyQualifiedName(this INamedTypeSymbol? namedTypeSymbol, bool withoutGenerics)
    {
        if (namedTypeSymbol is null)
            return null;
        else if (namedTypeSymbol is ITypeParameterSymbol typeParamSymbol)
            return $"{typeParamSymbol.DeclaringType.GetFullyQualifiedName(true)}@{namedTypeSymbol.Name}";

        return $"{(namedTypeSymbol.ContainingType is INamedTypeSymbol containingType ? containingType.GetFullyQualifiedName() : namedTypeSymbol.ContainingNamespace.GetFullNamespaceName())}.{namedTypeSymbol.Name}{(!withoutGenerics && namedTypeSymbol.IsGenericType ? $"<{string.Join(", ", (namedTypeSymbol.IsUnboundGenericType ? namedTypeSymbol.TypeParameters.Cast<ITypeSymbol>() : namedTypeSymbol.TypeArguments).Select(p => p.GetFullyQualifiedName()))}>" : "")}";
    }
    public static IEnumerable<INamedTypeSymbol> GetAllTypeMembers(this INamedTypeSymbol namedTypeSymbol)
    {
        foreach (INamedTypeSymbol child in namedTypeSymbol.GetTypeMembers().SelectMany(t => t.GetAllTypeMembers()))
            yield return child;
    }
    public static IEnumerable<INamedTypeSymbol> GetAllTypeMembers(this INamespaceSymbol namespaceSymbol)
    {
        foreach (INamedTypeSymbol child in namespaceSymbol.GetMembers().OfType<INamedTypeSymbol>())
        {
            yield return child;
            foreach (INamedTypeSymbol nestedChild in child.GetAllTypeMembers())
                yield return nestedChild;
        }

        foreach (INamedTypeSymbol childNamespaceTypeSymbol in namespaceSymbol.GetNamespaceMembers().SelectMany(ns => ns.GetAllTypeMembers()))
            yield return childNamespaceTypeSymbol;
    }
    public static IEnumerable<INamedTypeSymbol> GetAllTypeMembers(this Compilation compilation) => compilation.GlobalNamespace.GetAllTypeMembers();

    public static bool HasParent(this ITypeSymbol typeSymbol, ITypeSymbol parentType)
    {
        string parentName = parentType.GetFullyQualifiedName()!;
        while (typeSymbol.BaseType is not null)
        {
            string baseName = typeSymbol.BaseType.GetFullyQualifiedName()!;
            if (baseName == parentName)
                return true;
            typeSymbol = typeSymbol.BaseType;
        }
        return false;
    }
    public static bool HasParent(this ITypeSymbol typeSymbol, string parentTypeFullName)
    {
        while (typeSymbol.BaseType is not null)
        {
            string baseName = typeSymbol.BaseType.GetFullyQualifiedName()!;
            if (baseName == parentTypeFullName)
                return true;
            typeSymbol = typeSymbol.BaseType;
        }
        return false;
    }

    public static bool HasImplementation(this ITypeSymbol typeSymbol)
    {
        return !typeSymbol.IsAbstract && typeSymbol.TypeKind is not TypeKind.Interface && typeSymbol is not ITypeParameterSymbol;
    }

    public static bool IsGenericParameter(this ITypeSymbol typeSymbol)
    {
        return typeSymbol is ITypeParameterSymbol;
    }

    public static IEnumerable<ITypeSymbol> GenericArgs(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
            return Enumerable.Empty<ITypeSymbol>();
        return namedTypeSymbol.IsUnboundGenericType ? namedTypeSymbol.TypeParameters : namedTypeSymbol.TypeArguments;
    }

    public static IEnumerable<ITypeSymbol> AllGenericArgs(this ITypeSymbol typeSymbol)
    {
        foreach (ITypeSymbol nestingGenericArg in typeSymbol.NestingTypes().Reverse().SelectMany(nestingType => nestingType.GenericArgs()))
            yield return nestingGenericArg;
        foreach (ITypeSymbol genericArg in typeSymbol.GenericArgs())
            yield return genericArg;
    }

    public static IEnumerable<INamedTypeSymbol> Ancestors(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
            yield break;
        foreach (var iface in namedTypeSymbol.Interfaces.Where(iface => iface is not null))
            yield return iface;
        while (namedTypeSymbol.BaseType is not null)
        {
            yield return namedTypeSymbol = namedTypeSymbol.BaseType;
            foreach (var iface in namedTypeSymbol.Interfaces.Where(iface => iface is not null))
                yield return iface;
        }
    }

    public static IEnumerable<ITypeSymbol> NestingTypes(this ITypeSymbol typeSymbol)
    {
        while (typeSymbol.ContainingType is INamedTypeSymbol nestingType)
        {
            yield return typeSymbol = nestingType;
        }
    }

    public static bool IsExternal(this ISymbol symbol, Compilation compilation)
    {
        return !AreAssembliesEqual(symbol.ContainingAssembly, compilation.Assembly);
    }

    public static bool IsNonPublic(this ISymbol symbol)
    {
        return symbol.DeclaredAccessibility != Accessibility.Public;
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
    public static bool AreAssembliesEqual(this IAssemblySymbol assembly, IAssemblySymbol equalAssembly)
    {
        return
            (assembly, equalAssembly) is (null, null)
            ||
            (
                (assembly, equalAssembly) is (not null, not null)
                && assembly.Name == equalAssembly.Name
                && assembly.TypeNames.Count == equalAssembly.TypeNames.Count
                &&
                    assembly
                    .TypeNames
                    .Zip(equalAssembly.TypeNames, (a, b) => (a, b))
                    .All(x => x.a == x.b)
            );
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
    public static IEnumerable<ISymbol> GetMembersAndInheritedMembers(this ITypeSymbol? typeSymbol)
    {
        while (typeSymbol != null)
        {
            foreach (ISymbol symbol in typeSymbol.GetMembers())
                yield return symbol;
            typeSymbol = typeSymbol.BaseType;
        }
    }
    public static IEnumerable<IPropertySymbol> PublicSetProperties(this ITypeSymbol typeSymbol)
    {
        return
            typeSymbol
            .GetMembersAndInheritedMembers()
            .OfType<IPropertySymbol>()
            .Where(prop => prop.SetMethod is IMethodSymbol setMethod
                        && setMethod.DeclaredAccessibility == Accessibility.Public);
    }
}
