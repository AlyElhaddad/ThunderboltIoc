using Microsoft.CodeAnalysis;

using System.Reflection;

namespace Thunderbolt.GeneratorAbstractions;

internal class TypeDescriptor : IEquatable<TypeDescriptor>
{
    private const int defaultMaxDepth = 5;
    private static readonly string ienumerableFullName;

    static TypeDescriptor()
    {
        ienumerableFullName = typeof(IEnumerable<>).GetFullyQualifiedName();
        //ienumerableFullName = ienumerableFullName.Substring(0, ienumerableFullName.IndexOf('<'));
    }

    public TypeDescriptor(
        string name,
        bool isImplemented,
        TypeDescriptor? collectionTypeArg,
        TypeDescriptor? genericTypeDefinition,
        bool isGenericParameter,
        IEnumerable<TypeDescriptor> genericArgs,
        bool isExternalNonPublicType,
        IEnumerable<IEnumerable<TypeDescriptor>> ctorsParamsTypes,
        IDictionary<string, TypeDescriptor> publicSetProperties,
        IEnumerable<TypeDescriptor> ancestors,
        IEnumerable<TypeDescriptor> nestingTypes)
    {
        Name = name;
        IsImplemented = isImplemented;
        CollectionTypeArg = collectionTypeArg;
        GenericTypeDefinition = genericTypeDefinition;
        IsGenericParameter = isGenericParameter;
        GenericArgs = genericArgs;
        IsExternalNonPublicType = isExternalNonPublicType;
        CtorsParamsTypes = ctorsParamsTypes;
        PublicSetProperties = publicSetProperties;
        Ancestors = ancestors;
        NestingTypes = nestingTypes;
    }

    public string Name { get; set; }
    public bool IsImplemented { get; set; }
    public TypeDescriptor? CollectionTypeArg { get; set; }
    public TypeDescriptor? GenericTypeDefinition { get; set; }
    public bool IsGenericParameter { get; set; }
    public bool IsGeneric => GenericArgs.Any();
    //public bool IsClosedGenericType => !IsNonClosedGenericType;
    public bool IsNonClosedGenericType => IsGenericParameter || GenericArgs.Any(arg => arg.IsNonClosedGenericType) || NestingTypes.Any(nestingType => nestingType.IsNonClosedGenericType);
    public IEnumerable<TypeDescriptor> GenericArgs { get; set; }

    public bool HasExternalNonPublicGenericArgs => GenericArgs.Any(arg => !arg.IsGenericParameter && arg.TendsToExternalNonPublic);
    public bool IsNestedInTypeThatHasExternalNonPublicGenericArgs => NestingTypes.Any(nestingType => nestingType.HasExternalNonPublicGenericArgs);
    public bool IsExternalNonPublicType { get; set; }
    public bool IsNestedInExternalNonPublicType => NestingTypes.Any(type => type.TendsToExternalNonPublic);
    public bool TendsToExternalNonPublic => IsExternalNonPublicType || IsNestedInExternalNonPublicType || HasExternalNonPublicGenericArgs || IsNestedInTypeThatHasExternalNonPublicGenericArgs;

    public IEnumerable<IEnumerable<TypeDescriptor>> CtorsParamsTypes { get; set; }
    public IDictionary<string, TypeDescriptor> PublicSetProperties { get; set; }
    public IEnumerable<TypeDescriptor> Ancestors { get; set; }
    public IEnumerable<TypeDescriptor> NestingTypes { get; set; }

    #region Import

    public static TypeDescriptor FromTypeSymbol(ITypeSymbol typeSymbol, Compilation compilation)
    {
        return FromTypeSymbol(typeSymbol, compilation, defaultMaxDepth);
    }
    private static TypeDescriptor FromTypeSymbol(ITypeSymbol typeSymbol, Compilation compilation, int maxDepth)
    {
        --maxDepth;

        INamedTypeSymbol? namedTypeSymbol = typeSymbol as INamedTypeSymbol;

        string name = typeSymbol.GetFullyQualifiedName()!;
        bool isImplemented = typeSymbol.HasImplementation();
        bool isGenericParameter = typeSymbol.IsGenericParameter();
        IEnumerable<TypeDescriptor> genericArgs = maxDepth <= 0 ? Enumerable.Empty<TypeDescriptor>() : typeSymbol.AllGenericArgs().Select(genericArg => FromTypeSymbol(genericArg, compilation));
        bool isExternalNonPublicType = typeSymbol.IsExternal(compilation) && typeSymbol.IsNonPublic();
        IEnumerable<IEnumerable<TypeDescriptor>> ctorsParamsTypes = maxDepth <= 0 ? Enumerable.Empty<IEnumerable<TypeDescriptor>>() : typeSymbol is not INamedTypeSymbol ? Enumerable.Empty<IEnumerable<TypeDescriptor>>() : namedTypeSymbol!.InstanceConstructors.Where(ctor => ctor.DeclaredAccessibility == Accessibility.Public).Select(ctor => ctor.Parameters.Select(ctorParam => FromTypeSymbol(ctorParam.Type, compilation)));
#pragma warning disable RS1024 // Compare symbols correctly
        IDictionary<string, TypeDescriptor> publicSetProperties = maxDepth <= 0 ? new Dictionary<string, TypeDescriptor>() : typeSymbol.PublicSetProperties().ToDictionary(prop => prop.Name, prop => FromTypeSymbol((prop.Type as INamedTypeSymbol)!, compilation));
#pragma warning restore RS1024 // Compare symbols correctly
        IEnumerable<TypeDescriptor> ancestors = maxDepth <= 0 ? Enumerable.Empty<TypeDescriptor>() : typeSymbol.Ancestors().Select(parent => FromTypeSymbol(parent, compilation));
        IEnumerable<TypeDescriptor> nestingTypes = maxDepth <= 0 ? Enumerable.Empty<TypeDescriptor>() : typeSymbol.NestingTypes().Select(nestingType => FromTypeSymbol(nestingType, compilation));

        TypeDescriptor? collectionTypeArg;
        //if ((name.StartsWith(ienumerableFullName)
        //        || ancestors.Any(ancestor => ancestor.Name.StartsWith(ienumerableFullName)))
        //    && genericArgs.Count() == 1
        //    && maxDepth > 0)
        if (name == ienumerableFullName
            && genericArgs.Count() == 1
            && maxDepth > 0)
        {
            collectionTypeArg = genericArgs.First();
        }
        else
        {
            collectionTypeArg = null;
        }

        TypeDescriptor? genericTypeDefinition;
        if (!isGenericParameter && genericArgs.Any() && namedTypeSymbol is not null && maxDepth > 0)
        {
            if (namedTypeSymbol.IsUnboundGenericType)
            {
                genericTypeDefinition = FromTypeSymbol(namedTypeSymbol, compilation, maxDepth);
            }
            else
            {
                genericTypeDefinition = FromTypeSymbol(namedTypeSymbol.ConstructUnboundGenericType(), compilation, maxDepth);
            }
        }
        else
        {
            genericTypeDefinition = null;
        }

        return new TypeDescriptor(
            name,
            isImplemented,
            collectionTypeArg,
            genericTypeDefinition,
            isGenericParameter,
            genericArgs,
            isExternalNonPublicType,
            ctorsParamsTypes,
            publicSetProperties,
            ancestors,
            nestingTypes);
    }
    public static TypeDescriptor FromType(Type type, Assembly homeAssembly)
    {
        return FromType(type, homeAssembly, defaultMaxDepth);
    }
    private static TypeDescriptor FromType(Type type, Assembly homeAssembly, int maxDepth)
    {
        --maxDepth;

        string name = type.GetFullyQualifiedName();
        bool isImplemented = type.HasImplementation();
        bool isGenericParameter = type.IsGenericParameter;
        IEnumerable<TypeDescriptor> genericArgs = maxDepth <= 0 ? Enumerable.Empty<TypeDescriptor>() : type.GetGenericArguments().Select(genericArg => FromType(genericArg, homeAssembly, maxDepth));
        bool isExternalNonPublicType = type.IsNotPublic && type.Assembly != homeAssembly;
        IEnumerable<IEnumerable<TypeDescriptor>> ctorsParamsTypes = maxDepth <= 0 ? Enumerable.Empty<IEnumerable<TypeDescriptor>>() : type.GetConstructors().Where(ctor => ctor.IsPublic).Select(ctor => ctor.GetParameters().Select(ctorParam => FromType(ctorParam.ParameterType, homeAssembly, maxDepth)));
        IDictionary<string, TypeDescriptor> publicSetProperties = maxDepth <= 0 ? new Dictionary<string, TypeDescriptor>() : type.PublicSetProperties().ToDictionary(prop => prop.Name, prop => FromType(prop.PropertyType, homeAssembly, maxDepth));
        IEnumerable<TypeDescriptor> ancestors = maxDepth <= 0 ? Enumerable.Empty<TypeDescriptor>() : type.Ancestors().Select(parent => FromType(parent, homeAssembly, maxDepth));
        IEnumerable<TypeDescriptor> nestingTypes = maxDepth <= 0 ? Enumerable.Empty<TypeDescriptor>() : type.NestingTypes().Select(nestingType => FromType(nestingType, homeAssembly, maxDepth));

        TypeDescriptor? collectionTypeArg;
        //if (!isImplemented
        //    && (name.StartsWith(ienumerableFullName)
        //        || ancestors.Any(ancestor => ancestor.Name.StartsWith(ienumerableFullName)))
        //    && genericArgs.Count() == 1)
        if (!isImplemented
            && name == ienumerableFullName
            && genericArgs.Count() == 1)
        {
            collectionTypeArg = genericArgs.First();
        }
        else
        {
            collectionTypeArg = null;
        }

        TypeDescriptor? genericTypeDefinition;
        if (!isGenericParameter && genericArgs.Any() && maxDepth > 0)
        {
            if (type.IsGenericTypeDefinition)
            {
                genericTypeDefinition = FromType(type, homeAssembly, maxDepth);
            }
            else
            {
                genericTypeDefinition = FromType(type.GetGenericTypeDefinition(), homeAssembly, maxDepth);
            }
        }
        else
        {
            genericTypeDefinition = null;
        }

        return new TypeDescriptor(
            name,
            isImplemented,
            collectionTypeArg,
            genericTypeDefinition,
            isGenericParameter,
            genericArgs,
            isExternalNonPublicType,
            ctorsParamsTypes,
            publicSetProperties,
            ancestors,
            nestingTypes);
    }
    #endregion

    #region Equality
    public override int GetHashCode()
        => Name?.GetHashCode() ?? 0;
    public override bool Equals(object obj)
        => obj is TypeDescriptor instance && Equals(instance);
    public bool Equals(TypeDescriptor other)
        => Name == other?.Name;
    public static bool operator ==(TypeDescriptor? left, TypeDescriptor? right) => left?.Equals(right!) == true;
    public static bool operator !=(TypeDescriptor? left, TypeDescriptor? right) => !(left == right);
    #endregion

    public override string ToString()
    {
        return Name;
    }

    public bool MatchesService(ServiceDescriptor serviceDescriptor)
    {
        return
            Name is not null
            &&
            (
            serviceDescriptor.ServiceType == this
            || CollectionTypeArg is not null
            || serviceDescriptor.ServiceType.GenericTypeDefinition == GenericTypeDefinition
            //|| CollectionTypeArg?.MatchesService(serviceDescriptor) == true
            || GenericTypeDefinition?.MatchesService(serviceDescriptor) == true
            );
    }
}
