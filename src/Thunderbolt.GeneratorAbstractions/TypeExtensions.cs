using System.Linq;
using System.Reflection;
using System.Text;

namespace Thunderbolt.GeneratorAbstractions;

internal static class TypeExtensions
{
    private static Type? enumerableTypeDef;
    private static Type EnumerableTypeDef => enumerableTypeDef ??= typeof(IEnumerable<>);
    private static MethodInfo? enumerableEmptyMethodDef;
    private static MethodInfo EnumerableEmptyMethodDef => enumerableEmptyMethodDef ??= typeof(Enumerable).GetMethod(nameof(Enumerable.Empty), BindingFlags.Static | BindingFlags.Public);
    internal static bool IsEnumerable(this Type type)
        => type.IsGenericType ? (type.IsGenericTypeDefinition ? type : type.GetGenericTypeDefinition()) == EnumerableTypeDef : false;
    internal static object EmptyEnumerable(this Type type)
    {
        return EnumerableEmptyMethodDef.MakeGenericMethod(type.GetGenericArguments()[0]).Invoke(null, null);
    }
    internal static string GetFullyQualifiedName(this Type type)
        => type.GetFullyQualifiedName(false);
    private static string GetFullyQualifiedName(this Type type, bool withoutGenerics)
    {
        string typeName = type.Name;
        int genericCharIndex = Math.Max(typeName.IndexOf('`'), typeName.IndexOf('<'));
        if (genericCharIndex >= 0)
            typeName = typeName.Substring(0, genericCharIndex);
        if (type.IsGenericParameter)
        {
            return $"{type.DeclaringType.GetFullyQualifiedName(true)}@{typeName}";
        }
        var originalType = type;
        StringBuilder builder = new(typeName);
        while (type.DeclaringType is not null)
        {
            type = type.DeclaringType;
            builder.Insert(0, $"{typeName}.");
        }
        builder.Insert(0, $"global::{type.Namespace}.");
        type = originalType;
        if (!withoutGenerics && (type.IsGenericType || type.IsGenericTypeDefinition))
        {
            typeName = builder.ToString();
            genericCharIndex = Math.Max(typeName.IndexOf('`'), typeName.IndexOf('<'));
            if (genericCharIndex >= 0)
                typeName = typeName.Substring(0, genericCharIndex);
            builder
                = new StringBuilder(typeName)
                .Append('<')
                .Append(string.Join(", ", type.GetGenericArguments().Select(t => t.GetFullyQualifiedName())))
                .Append('>');
        }
        string result = builder.ToString();
        return result;
    }

    internal static bool TendsToExternalNonPublic(this Type type, Assembly assembly)
    {
        return
            (!type.IsPublic && type.Assembly != assembly)
            || type.GetGenericArguments().Any(genArg => !genArg.IsGenericParameter && genArg.TendsToExternalNonPublic(assembly))
            || type.NestingTypes().Any(type => type.TendsToExternalNonPublic(assembly));
    }

    internal static bool IsNonClosedGenericType(this Type type)
    {
        return
            type.IsGenericType
            && (type.IsGenericTypeDefinition
                || type.GetGenericArguments().Any(genArg => genArg.IsGenericParameter || genArg.IsNonClosedGenericType()));
    }

    public static bool HasImplementation(this Type type)
    {
        return !type.IsInterface && !type.IsAbstract && !type.IsGenericParameter;
    }

    internal static IEnumerable<Type> Ancestors(this Type type)
    {
        foreach (var iface in type.GetInterfaces())
            yield return iface;
        while (type.BaseType is Type baseType)
        {
            yield return type = baseType;
        }
    }
    internal static IEnumerable<Type> NestingTypes(this Type type)
    {
        if (type.IsGenericParameter)
            yield break;
        while (type.DeclaringType is Type nestingType)
        {
            yield return type = nestingType;
        }
    }

    internal static IEnumerable<PropertyInfo> PublicSetProperties(this Type type)
    {
        return
            type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
            .Where(prop => prop.SetMethod?.IsPublic == true);
    }
}
