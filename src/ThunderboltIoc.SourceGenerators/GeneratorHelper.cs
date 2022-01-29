using Microsoft.CodeAnalysis;

using System.Text;

namespace ThunderboltIoc.SourceGenerators;

internal static class GeneratorHelper
{
    private static string GenerateTypeCtorCall(INamedTypeSymbol type, IEnumerable<ServiceDescriptor> allServices)
    {
        //new T(resolver.Get<TDependency1>(), resolver.Get<TDependency2>())

        string fullyQualifiedName = type.GetFullyQualifiedName();
        var ctor = DependencyHelper.FindBestCtors(type, allServices).First();

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

    private static string GenerateImplSelector(INamedTypeSymbol type, IEnumerable<INamedTypeSymbol> serviceImpls, IEnumerable<ServiceDescriptor> allServices)
    {
        StringBuilder builder = new("Type implType = implSelector();");
        foreach (var serviceImp in serviceImpls)
        {
            builder.AppendLine();
            string returned;
            string serviceImpName = serviceImp.GetFullyQualifiedName();
            if (allServices.FirstOrDefault(serviceDescriptor => serviceDescriptor.ServiceSymbol.GetFullyQualifiedName() == serviceImpName) is ServiceDescriptor serviceDescriptor && serviceDescriptor.ServiceSymbol is not null)
            {
                returned =
$@"resolver.Get<{serviceDescriptor.ServiceSymbol.GetFullyQualifiedName()}>()";
            }
            else
            {
                returned = GenerateTypeCtorCall(serviceImp, allServices);
            }
            builder.Append($@"if (typeof({serviceImp.GetFullyQualifiedName()}) == implType) return {returned};");
        }
        builder.AppendLine();

        builder.Append($@"throw new InvalidOperationException(string.Format(""'{{0}}' is not a staticly registered implementation for a service of type '{{1}}'."", implType, ""{type.GetFullyQualifiedName().RemovePrefix(Consts.global)}""));");
        return builder.ToString();
    }

    private static string GenerateDictate(ServiceDescriptor serviceDescriptor, IEnumerable<ServiceDescriptor> allServices)
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
{GenerateImplSelector(serviceDescriptor.ServiceSymbol, serviceDescriptor.ImplSelectorSymbols, allServices).AddIndentation(1)}
}});";
        }
        if (serviceDescriptor.ImplSymbol is not null)
        {
            string implName = serviceDescriptor.ImplSymbol.GetFullyQualifiedName();
            if (allServices.FirstOrDefault(service => service.ServiceSymbol.GetFullyQualifiedName() == implName) is ServiceDescriptor implServiceDescriptor && implServiceDescriptor.ServiceSymbol is not null)
            {
                return
$@"dictator.Dictate<{fullyQualifiedName}>((resolver, implSelector, userFactory) => resolver.Get<{implServiceDescriptor.ServiceSymbol.GetFullyQualifiedName()}>());";
            }
        }
        return
$@"dictator.Dictate<{fullyQualifiedName}>((resolver, implSelector, userFactory) => {GenerateTypeCtorCall(serviceDescriptor.ImplSymbol ?? serviceDescriptor.ServiceSymbol, allServices)});";
    }
    private static string GenerateDictates(IEnumerable<ServiceDescriptor> allTypes, IEnumerable<ServiceDescriptor> allServices)
    {
        return string.Join(Environment.NewLine, allTypes.Select(item => GenerateDictate(item, allServices)));
    }

    private static string GenerateStaticRegistrations(IEnumerable<ServiceDescriptor> allTypes)
    {
        return string.Join(Environment.NewLine,
            allTypes.Select(reg =>
            $"reg.Add{(reg.Lifetime switch { 0 => Consts.Singleton, 1 => Consts.Scoped, 2 => Consts.Transient, _ => "" })}<{reg.ServiceSymbol.GetFullyQualifiedName()}{(reg.ImplSymbol is null ? "" : $", {reg.ImplSymbol.GetFullyQualifiedName()}")}>();"));
    }

    internal static string GenerateDictateServiceFactories(IEnumerable<ServiceDescriptor> allTypes, IEnumerable<ServiceDescriptor> allServices)
    {
        return !allTypes.Any() ? "" :
$@"protected override void DictateServiceFactories({Consts.IIocDictatorTypeFullName} dictator)
{{
{GenerateDictates(allTypes, allServices).AddIndentation(1)}
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
