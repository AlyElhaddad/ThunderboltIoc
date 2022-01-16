using Microsoft.CodeAnalysis;

using System.Text.RegularExpressions;

namespace ThunderboltIoc.SourceGenerators;

internal static class AttributeGeneratorHelper
{
    private class SymbolValueTupleEqualityComparer : IEqualityComparer<(INamedTypeSymbol, INamedTypeSymbol?, int)>
    {
        private static readonly EqualityComparer<string> comparer = EqualityComparer<string>.Default;
        public static readonly SymbolValueTupleEqualityComparer Default = new();

        private SymbolValueTupleEqualityComparer() { }

        public bool Equals((INamedTypeSymbol, INamedTypeSymbol?, int) x, (INamedTypeSymbol, INamedTypeSymbol?, int) y)
        {
            return comparer.Equals(x.Item1.GetFullyQualifiedName(), y.Item1.GetFullyQualifiedName());
        }

        public int GetHashCode((INamedTypeSymbol, INamedTypeSymbol?, int) obj)
        {
            return comparer.GetHashCode(obj.Item1.GetFullyQualifiedName());
        }
    }

    private static IEnumerable<((AttributeData data, string attrTypeName) attr, INamedTypeSymbol type)> GetTypeAttributes(Compilation compilation)
    {
        var attrTypes = new[]
        {
            Consts.includeAttrName,
            Consts.excludeAttrName
        };
        return
            compilation.GetAllTypeMembers()
            .SelectMany(type => type.GetAttributes().Select(attr => (attr: (data: attr, attrTypeName: attr.AttributeClass?.GetFullyQualifiedName()), type)))
            .Where(item => attrTypes.Any(attrType => item.attr.attrTypeName == attrType))
            .OfType<((AttributeData, string), INamedTypeSymbol)>();
    }

    private static IEnumerable<(AttributeData data, string attrTypeName)> GetAssemblyAttributes(Compilation compilation)
    {
        var attrTypes = new[]
        {
            Consts.regexIncludeAttrName,
            Consts.regexExcludeAttrName
        };
        return
            compilation.Assembly.GetAttributes()
            .Select(attr => (attr, attrTypeName: attr.AttributeClass?.GetFullyQualifiedName()))
            .Where(item => attrTypes.Any(attrType => attrType == item.attrTypeName))
            .OfType<(AttributeData, string)>();
    }

    private static IEnumerable<(INamedTypeSymbol type, INamedTypeSymbol? impl, int serviceLifetime)> IncludedTypes(Compilation compilation)
    {
        var allTypes = compilation.GetAllTypeMembers();
        var typeAttrs = GetTypeAttributes(compilation);

        List<(INamedTypeSymbol type, INamedTypeSymbol? impl, int serviceLifetime)> inclusions = new();
        List<INamedTypeSymbol> exclusions = new();
        foreach (var (attr, type) in typeAttrs)
        {
            switch (attr.attrTypeName)
            {
                case Consts.includeAttrName:
                    INamedTypeSymbol? impl = attr.data.ConstructorArguments.Where(arg => arg.Kind == TypedConstantKind.Type).Select(arg => arg.Type as INamedTypeSymbol).FirstOrDefault(t => t is not null);
                    int serviceLifetime = attr.data.ConstructorArguments.Where(arg => arg.Kind == TypedConstantKind.Enum).Select(arg => (int)arg.Value).First();
                    inclusions.Add((type, impl, serviceLifetime));
                    bool registerChilds = (attr.data.ConstructorArguments.Select(arg => arg.Value).FirstOrDefault(arg => arg is bool) as bool?) ?? false;
                    if (registerChilds)
                    {
                        inclusions.AddRange(allTypes.Where(t => t.HasParent(type)).Select(t => (t, impl, serviceLifetime)));
                    }
                    break;
                case Consts.excludeAttrName:
                    exclusions.Add(type);
                    bool excludeChilds = (attr.data.ConstructorArguments.Select(arg => arg.Value).FirstOrDefault(arg => arg is bool) as bool?) ?? false;
                    if (excludeChilds)
                    {
                        exclusions.AddRange(allTypes.Where(t => t.HasParent(type)));
                    }
                    break;
                default:
                    continue;
            }
        }

        return inclusions.WhereIf(exclusions.Any(), incl => !exclusions.Any(excl => incl.type.GetFullyQualifiedName() == excl.GetFullyQualifiedName()));
    }

    private static IEnumerable<(INamedTypeSymbol type, INamedTypeSymbol? impl, int serviceLifetime)> IncludedRegexTypes(Compilation compilation)
    {
        var allTypes = compilation.GetAllTypeMembers();
        var assemblyAttrs = GetAssemblyAttributes(compilation);
        List<(INamedTypeSymbol type, string typeFullName, INamedTypeSymbol? impl, int servcieLifetime)> inclusions = new();
        List<(INamedTypeSymbol type, string typeFullName)> exclusions = new();
        foreach (var (data, attrTypeName) in assemblyAttrs.Where(attr => attr.attrTypeName is Consts.regexIncludeAttrName or Consts.regexExcludeAttrName))
        {
            string? pattern;
            switch (attrTypeName)
            {
                case Consts.regexIncludeAttrName:
                    pattern = (string?)data.ConstructorArguments[1].Value;
                    int serviceLifetime = (int?)data.ConstructorArguments[0].Value ?? default;
                    if (pattern is null) continue;
                    pattern = $@"({Consts.global})?{pattern}";
                    IEnumerable<(INamedTypeSymbol type, string typeFullName, INamedTypeSymbol? typeImpl, int serviceLifetime)> typesToInclude;
                    var types
                        = allTypes.Select(type => (type, typeFullName: type.GetFullyQualifiedName(), serviceLifetime))
                        .Where(item => Regex.IsMatch(item.typeFullName, pattern));
                    if (data.ConstructorArguments.Length == 4)
                    {
                        if ((string?)data.ConstructorArguments[2].Value is not string implPattern || (string?)data.ConstructorArguments[3].Value is not string joinKey)
                            continue;
                        var implTypes
                            = allTypes.Select(type => (type, typeFullName: type.GetFullyQualifiedName(), serviceLifetime))
                            .Where(item => Regex.IsMatch(item.typeFullName, implPattern));
                        typesToInclude
                            = types.Join(implTypes, item => Regex.Match(item.typeFullName, joinKey)?.Value, item => Regex.Match(item.typeFullName, joinKey)?.Value, (outer, inner) => (outer.type, outer.typeFullName, inner.type ?? null, outer.serviceLifetime));
                    }
                    else
                    {
                        typesToInclude = types.Select(t => (t.type, t.typeFullName, default(INamedTypeSymbol?), t.serviceLifetime));
                    }
                    inclusions.AddRange(typesToInclude);
                    break;
                case Consts.regexExcludeAttrName:
                    pattern = (string?)data.ConstructorArguments[0].Value;
                    if (pattern is null) continue;
                    pattern = $@"({Consts.global})?{pattern}";
                    exclusions.AddRange(
                        allTypes.Select(type => (type, typeFullName: type.GetFullyQualifiedName()))
                        .Where(item => Regex.IsMatch(item.typeFullName, pattern)));
                    break;
                default:
                    continue;
            }
        }

        return inclusions
            .WhereIf(exclusions.Any(), incl => !exclusions.Any(excl => excl.typeFullName == incl.typeFullName))
            .Select(incl => (incl.type, incl.impl, incl.servcieLifetime));
    }

    internal static IEnumerable<(INamedTypeSymbol type, INamedTypeSymbol? impl, int serviceLifetime)> AllIncludedTypes(Compilation compilation)
    {
        return IncludedTypes(compilation)
            .Union(IncludedRegexTypes(compilation), SymbolValueTupleEqualityComparer.Default);
    }
}
