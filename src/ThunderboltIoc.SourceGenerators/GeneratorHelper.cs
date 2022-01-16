using Microsoft.CodeAnalysis;

using System.Text;

namespace ThunderboltIoc.SourceGenerators
{
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
    
        private static string GenerateDictate(INamedTypeSymbol type, INamedTypeSymbol? impl, IEnumerable<INamedTypeSymbol>? selectorImpls, bool hasFactory)
        {
            //this way may seem a bit more cleaner but it's a lot of hassle to write and not worthy of time for version 1
            //SyntaxFactory.ExpressionStatement(
            //    SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
            //        kind: SyntaxKind.SimpleMemberAccessExpression,
            //        SyntaxFactory.IdentifierName("factories"),
            //        SyntaxFactory.IdentifierName("Add")))
            //    .WithArgumentList(SyntaxFactory.ArgumentList(new SeparatedSyntaxList<ArgumentSyntax>().));

            string fullyQualifiedName = type.GetFullyQualifiedName();
            if (type.TypeKind is not TypeKind.Class and not TypeKind.Interface and not TypeKind.Struct)
                throw new NotSupportedException($"Only interfaces, classes and structs are supported. The registered type '{fullyQualifiedName}', however, is '{type.TypeKind}'.");

            if (hasFactory)
            {
                return
$@"dictator.Dictate<{fullyQualifiedName}>((resolver, implSelector, userFactory) => userFactory());";
            }
            if (selectorImpls is not null)
            {
                return
$@"dictator.Dictate<{fullyQualifiedName}>((resolver, implSelector, userFactory) =>
{{
{GenerateImplSelectorCtors(type, selectorImpls).AddIndentation(1)}
}});";
            }
            return
$@"dictator.Dictate<{fullyQualifiedName}>((resolver, implSelector, userFactory) => {GenerateTypeCtorCall(impl ?? type)});";
        }
        
        private static string GenerateDictates(IEnumerable<(int? lifetime, INamedTypeSymbol service, INamedTypeSymbol? serviceImpl, IEnumerable<INamedTypeSymbol>? selectorImpls, bool hasFactory, bool staticRegister)> allTypes)
        {
            return string.Join(Environment.NewLine, allTypes.Select(item => GenerateDictate(item.service, item.serviceImpl, item.selectorImpls, item.hasFactory)));
        }
     
        private static string GenerateStaticRegistrations(IEnumerable<(int? lifetime, INamedTypeSymbol service, INamedTypeSymbol? serviceImpl, IEnumerable<INamedTypeSymbol>? selectorImpls, bool hasFactory, bool staticRegister)> allTypes)
        {
            return string.Join(Environment.NewLine,
                allTypes.Select(reg =>
                $"reg.Add{(reg.lifetime switch { 0 => Consts.Singleton, 1 => Consts.Scoped, 2 => Consts.Transient, _ => ""})}<{reg.service.GetFullyQualifiedName()}{(reg.serviceImpl is null ? "" : $", {reg.serviceImpl.GetFullyQualifiedName()}")}>();"));
        }
      
        internal static string GenerateDictateServiceFactories(IEnumerable<(int? lifetime, INamedTypeSymbol service, INamedTypeSymbol? serviceImpl, IEnumerable<INamedTypeSymbol>? selectorImpls, bool hasFactory, bool staticRegister)> allTypes)
        {
            return !allTypes.Any() ? "" :
$@"protected override void DictateServiceFactories({Consts.IIocDictatorTypeFullName} dictator)
{{
{GenerateDictates(allTypes).AddIndentation(1)}
}}";
        }
     
        internal static string GenerateStaticRegister(IEnumerable<(int? lifetime, INamedTypeSymbol service, INamedTypeSymbol? serviceImpl, IEnumerable<INamedTypeSymbol>? selectorImpls, bool hasFactory, bool staticRegister)> allTypes)
        {
            return !allTypes.Any(item => item.staticRegister) ? "" :
$@"protected override void StaticRegister({Consts.IIocRegistrarTypeFullName} reg)
{{
{GenerateStaticRegistrations(allTypes.Where(item => item.staticRegister)).AddIndentation(1)}
}}";
        }
    }
}
