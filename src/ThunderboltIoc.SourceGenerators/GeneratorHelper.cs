using Microsoft.CodeAnalysis;

using System.Text;

namespace ThunderboltIoc.SourceGenerators;

internal static class GeneratorHelper
{
    private static string GenerateTypeCtorCall(INamedTypeSymbol type)
    {
        //new T(resolver.Get<TDependency1>(), resolver.Get<TDependency2>())

        string fullyQualifiedName = type.GetFullyQualifiedName();
        if (type.InstanceConstructors.SingleOrDefault() is not IMethodSymbol ctor
            || ctor.DeclaredAccessibility != Accessibility.Public)
            throw new InvalidOperationException($"Cannot generate a factory for type '{fullyQualifiedName}'. Registered types must have one and only one public constructor.");

        StringBuilder builder = new("new ");
        builder.Append(fullyQualifiedName);
        builder.Append('(');
        bool isFirst = true;
        foreach (var param in ctor.Parameters)
        {
            if (param.Type is not INamedTypeSymbol paramType)
                throw new InvalidOperationException($"Cannot infer the type of the parameter '{param.Name}' in the constructor of the type '{fullyQualifiedName}'.");

            if (isFirst)
                isFirst = false;
            else
                builder.Append(", ");
            builder.Append("resolver.Get<");
            builder.Append(paramType.GetFullyQualifiedName());
            builder.Append(">()");
        }
        builder.Append(')');
        return builder.ToString();
    }

    private static string GenerateImplSelectorCtors(INamedTypeSymbol type, IEnumerable<INamedTypeSymbol> serviceImpls)
    {
        StringBuilder builder = new("Type implType = implSelector();");
        foreach (var serviceImp in serviceImpls)
        {
            builder.AppendLine();
            builder.Append($@"if (typeof({serviceImp.GetFullyQualifiedName()}) == implType) return {GenerateTypeCtorCall(serviceImp)};");
        }
        builder.AppendLine();

        builder.Append($@"throw new InvalidOperationException(string.Format(""'{{0}}' is not a staticly registered implementation for a service of type '{{1}}'."", implType, ""{type.GetFullyQualifiedName().RemovePrefix(Consts.global)}""));");
        return builder.ToString();
    }

    private static string GenerateDictate(ServiceDescriptor serviceDescriptor)
    {
        //this way may seem a bit more cleaner but it's a lot of hassle to write and not worthy of time for version 1
        //SyntaxFactory.ExpressionStatement(
        //    SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
        //        kind: SyntaxKind.SimpleMemberAccessExpression,
        //        SyntaxFactory.IdentifierName("factories"),
        //        SyntaxFactory.IdentifierName("Add")))
        //    .WithArgumentList(SyntaxFactory.ArgumentList(new SeparatedSyntaxList<ArgumentSyntax>().));

        string fullyQualifiedName = serviceDescriptor.ServiceSymbol.GetFullyQualifiedName();
        if (serviceDescriptor.ServiceSymbol.TypeKind is not TypeKind.Class and not TypeKind.Interface and not TypeKind.Struct)
            throw new NotSupportedException($"Only interfaces, classes and structs are supported. The registered type '{fullyQualifiedName}', however, is '{serviceDescriptor.ServiceSymbol.TypeKind}'.");

        if (serviceDescriptor.HasFactory)
        {
            return
$@"dictator.Dictate<{fullyQualifiedName}>((resolver, implSelector, userFactory) => userFactory());";
        }
        if (serviceDescriptor.ImplSelectorSymbols is not null)
        {
            return
$@"dictator.Dictate<{fullyQualifiedName}>((resolver, implSelector, userFactory) =>
{{
{GenerateImplSelectorCtors(serviceDescriptor.ServiceSymbol, serviceDescriptor.ImplSelectorSymbols).AddIndentation(1)}
}});";
        }
        return
$@"dictator.Dictate<{fullyQualifiedName}>((resolver, implSelector, userFactory) => {GenerateTypeCtorCall(serviceDescriptor.ImplSymbol ?? serviceDescriptor.ServiceSymbol)});";
    }
    private static string GenerateDictates(IEnumerable<ServiceDescriptor> allTypes)
    {
        return string.Join(Environment.NewLine, allTypes.Select(item => GenerateDictate(item)));
    }

    private static string GenerateStaticRegistrations(IEnumerable<ServiceDescriptor> allTypes)
    {
        return string.Join(Environment.NewLine,
            allTypes.Select(reg =>
            $"reg.Add{(reg.Lifetime switch { 0 => Consts.Singleton, 1 => Consts.Scoped, 2 => Consts.Transient, _ => "" })}<{reg.ServiceSymbol.GetFullyQualifiedName()}{(reg.ImplSymbol is null ? "" : $", {reg.ImplSymbol.GetFullyQualifiedName()}")}>();"));
    }

    internal static string GenerateDictateServiceFactories(IEnumerable<ServiceDescriptor> allTypes)
    {
        return !allTypes.Any() ? "" :
$@"protected override void DictateServiceFactories({Consts.IIocDictatorTypeFullName} dictator)
{{
{GenerateDictates(allTypes).AddIndentation(1)}
}}";
    }

    internal static string GenerateStaticRegister(IEnumerable<ServiceDescriptor> allTypes)
    {
        return !allTypes.Any(item => item.RegisteredByAttribute) ? "" :
$@"protected override void StaticRegister({Consts.IIocRegistrarTypeFullName} reg)
{{
{GenerateStaticRegistrations(allTypes.Where(item => item.RegisteredByAttribute)).AddIndentation(1)}
}}";
    }
}
