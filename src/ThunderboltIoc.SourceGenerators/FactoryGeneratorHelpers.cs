using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

using System.Text;

namespace ThunderboltIoc.SourceGenerators;

internal static class FactoryGeneratorHelpers
{

    private static IEnumerable<ClassDeclarationSyntax> ContainerDescendantClassDeclarations(Compilation compilation)
    {
        ITypeSymbol containerType = compilation.GetTypeByFullName(Consts.IocContainerTypeFullName) ?? throw new MissingMemberException(); //compilation.GetAllTypeMembers().Single(t => t.GetFullyQualifiedName() == IocContainerTypeFullName);
        return
            compilation
            .SyntaxTrees
            .SelectMany(st => st.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            .Where(d => compilation.GetSemanticModel(d.SyntaxTree).GetDeclaredSymbol(d) is ITypeSymbol classSymbol
                        && classSymbol.HasParent(containerType));
    }

    private static IEnumerable<MethodDeclarationSyntax> ContainerOverriddenRegisterMethods(Compilation compilation)
    {
        SemanticModel? semanticModel = null;
        return
            ContainerDescendantClassDeclarations(compilation)
            .SelectMany(c => c.GetMethods(Consts.RegisterMethodName, Consts.@public, Consts.@override))
            .Where(m =>
            {
                if (semanticModel is null)
                    semanticModel = compilation.GetSemanticModel(m.SyntaxTree);
                return semanticModel.GetDeclaredSymbol(m) is IMethodSymbol method
                        && !method.TypeParameters.Any()
                        && method.Parameters.Length == 1
                        && method.Parameters[0].Type.GetFullyQualifiedName().RemovePrefix(Consts.global) == Consts.IIocRegistrarTypeFullName.RemovePrefix(Consts.global);
            });
    }

    private static IEnumerable<INamedTypeSymbol> TypesToRegister(Compilation compilation)
    {
        IEnumerable<MethodDeclarationSyntax> registerDecls = ContainerOverriddenRegisterMethods(compilation);
        HashSet<string> iocRegistrarMethodFullNames = new(compilation.GetTypeByFullName(Consts.IIocRegistrarTypeFullName)?.GetMembers().OfType<IMethodSymbol>().Where(m => !m.Name.EndsWith("Factory")).Select(m => $"{Consts.IIocRegistrarTypeFullName}.{m.Name}") ?? Enumerable.Empty<string>());
        //iocRegistrarMethodFullNames (except factory methods)

        foreach (MethodDeclarationSyntax registerDecl in registerDecls)
        {
            SemanticModel semanticModel = compilation.GetSemanticModel(registerDecl.SyntaxTree);
            BlockSyntax blockStatement = registerDecl.Body ?? throw new MissingMemberException();
            IEnumerable<InvocationExpressionSyntax> invExpressions
                = blockStatement.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Where(inv => semanticModel.GetOperation(inv) is IInvocationOperation invOp
                              && iocRegistrarMethodFullNames.Contains($"{Consts.IIocRegistrarTypeFullName}.{invOp.TargetMethod.OriginalDefinition.Name}"));
            foreach (InvocationExpressionSyntax syntax in invExpressions)
            {
                if ((syntax.Expression as MemberAccessExpressionSyntax)?.Name is not GenericNameSyntax genericName)
                    continue;
                if (!syntax.ArgumentList.Arguments.Any()
                    && genericName.TypeArgumentList.Arguments.Count is 1 or 2)
                {
                    //single argument signatures are reserved for factory registrations
                    //1 or 2 generic args - a redundant/double check just to be sure
                    TypeSyntax typeArgument = genericName.TypeArgumentList.Arguments.Last();
                    if (semanticModel.GetSpeculativeTypeInfo(typeArgument.SpanStart, typeArgument, SpeculativeBindingOption.BindAsTypeOrNamespace).Type is INamedTypeSymbol regType)
                    {
                        yield return regType;
                    }
                }
                else if (syntax.ArgumentList.Arguments.Count == 1)
                {
                    //implSelector signature
                    ArgumentSyntax arg = syntax.ArgumentList.Arguments[0];
                    BlockSyntax implSelectorBlockStatement;
                    if (arg.Expression is ParenthesizedLambdaExpressionSyntax lambdaExpr)
                    {
                        implSelectorBlockStatement = lambdaExpr.Block ?? throw new MissingMethodException();
                    }
                    else if (arg.Expression is AnonymousMethodExpressionSyntax anonymousExpr)
                    {
                        implSelectorBlockStatement = anonymousExpr.Block ?? throw new MissingMethodException();
                    }
                    else
                    {
                        throw new MissingMethodException();
                    }
                    foreach (ReturnStatementSyntax returnStatement in implSelectorBlockStatement.DescendantNodes().OfType<ReturnStatementSyntax>())
                    {
                        if (returnStatement.Expression is not TypeOfExpressionSyntax typeOfExpr)
                            continue;
                        if (semanticModel.GetSpeculativeTypeInfo(typeOfExpr.Type.SpanStart, typeOfExpr.Type, SpeculativeBindingOption.BindAsTypeOrNamespace).Type is INamedTypeSymbol regType)
                        {
                            yield return regType;
                        }
                    }
                }
            }
        }
    }

    internal static string GenerateTypeFactory(INamedTypeSymbol type)
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
        if (type.InstanceConstructors.FirstOrDefault() is not IMethodSymbol ctor
            || ctor.DeclaredAccessibility != Accessibility.Public)
            throw new InvalidOperationException($"Cannot generate a factory for type '{fullyQualifiedName}'. Registered types must have one and only one public constructor.");
        StringBuilder factoryBuilder = new();
        //factories.Add(typeof(T), resolver => new T(resolver.Get<T>(tDependency1), resolver.Get<T>(tDependency2)));
        factoryBuilder.Append("factories.Add(typeof(");
        factoryBuilder.Append(fullyQualifiedName);
        factoryBuilder.Append("), resolver => new ");
        factoryBuilder.Append(fullyQualifiedName);
        factoryBuilder.Append("(");
        bool isFirst = true;
        foreach (var param in ctor.Parameters)
        {
            if (param.Type is not INamedTypeSymbol paramType)
                throw new InvalidOperationException($"Cannot infer the type of the parameter '{param.Name}' in the constructor of the type '{fullyQualifiedName}'.");

            if (isFirst)
                isFirst = false;
            else
                factoryBuilder.Append(", ");

            factoryBuilder.Append("resolver.Get<");
            factoryBuilder.Append(paramType.GetFullyQualifiedName());
            factoryBuilder.Append(">()");
        }
        factoryBuilder.Append("));");
        return factoryBuilder.ToString();
    }

    internal static void GenerateAddFactoriesForRegisteredTypes(GeneratorExecutionContext context)
    {
        IEnumerable<INamedTypeSymbol> processedTypes = TypesToRegister(context.Compilation);
        string factories = string.Join(Environment.NewLine, processedTypes.Select(t => $"\t\t{GenerateTypeFactory(t)}"));
        string source = @$"namespace {Consts.mainNs};

internal static partial class Factory
{{
    static partial void AddFactories()
    {{
{factories}
    }}
}}";
        context.AddSource("Factory.generated.cs", source);
    }
}

