using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

using System.Text;

namespace ThunderboltIoc.SourceGenerators;

internal static class FactoryGeneratorHelpers
{
    private static IEnumerable<(MethodDeclarationSyntax methodDecl, SemanticModel semanticModel)> GetDeclarationOverriddenRegisterMethod(IEnumerable<(ClassDeclarationSyntax declaration, SemanticModel semanticModel)> symbolDeclarations, INamedTypeSymbol registrarTypeSymbol)
    {
        return
            symbolDeclarations
            .SelectMany(item => item.declaration.GetDeclaredMethods(Consts.RegisterMethodName, Consts.@protected, Consts.@override).Select(m => (methodDecl: m, item.semanticModel)))
            .Where(item =>
            {
                return item.semanticModel.GetDeclaredSymbol(item.methodDecl) is IMethodSymbol method
                        && !method.TypeParameters.Any()
                        && method.Parameters.Length == 1
                        && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, registrarTypeSymbol);
            });
    }

    private static IEnumerable<INamedTypeSymbol> TypesToRegister(IEnumerable<(MethodDeclarationSyntax methodDecl, SemanticModel semanticModel)> overriddenRegisterDeclarations, HashSet<IMethodSymbol> registrarNonFactoryMethods)
    {
        foreach (var (registerDecl, semanticModel) in overriddenRegisterDeclarations)
        {
            BlockSyntax blockStatement = registerDecl.Body ?? throw new MissingMemberException();
            IEnumerable<InvocationExpressionSyntax> invExpressions
                = blockStatement.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Where(inv => semanticModel.GetOperation(inv) is IInvocationOperation invOp
                              && registrarNonFactoryMethods.Contains(invOp.TargetMethod.OriginalDefinition));
            foreach (InvocationExpressionSyntax syntax in invExpressions)
            {
                if ((syntax.Expression as MemberAccessExpressionSyntax)?.Name is not GenericNameSyntax genericName)
                    continue;
                if (!syntax.ArgumentList.Arguments.Any()
                    && genericName.TypeArgumentList.Arguments.Count is 1 or 2)
                {
                    //single argument signatures are reserved for factory registrations
                    //1 or 2 generic args - a redundant/double check just to be sure
                    TypeSyntax typeArgument = genericName.TypeArgumentList.Arguments.Last(); //we will be creating a factory only for the implementation, which will be the first (and last) if single arg or the second (and last) if double args
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
                    { // () => { }
                        implSelectorBlockStatement = lambdaExpr.Block ?? throw new MissingMethodException();
                    }
                    else if (arg.Expression is AnonymousMethodExpressionSyntax anonymousExpr)
                    { // delegate { }
                        implSelectorBlockStatement = anonymousExpr.Block ?? throw new MissingMethodException();
                    }
                    else
                    {
                        continue; //better than failure for now.
                        //throw new MissingMethodException();
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
        if (type.InstanceConstructors.SingleOrDefault() is not IMethodSymbol ctor
            || ctor.DeclaredAccessibility != Accessibility.Public)
            throw new InvalidOperationException($"Cannot generate a factory for type '{fullyQualifiedName}'. Registered types must have one and only one public constructor.");
        StringBuilder factoryBuilder = new();
        //dictator.Dictate(typeof(T), resolver => new T(resolver.Get<TDependency1>(), resolver.Get<TDependency2>()));
        factoryBuilder.Append("dictator.Dictate(typeof(");
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

    internal static void GenerateAddFactoriesForRegisteredTypes(GeneratorExecutionContext context, INamedTypeSymbol registrarTypeSymbol, HashSet<IMethodSymbol> registrarNonFactoryMethods)
    {
        if (context.SyntaxContextReceiver is not SyntaxContextReceiver syntaxContextReceiver)
            return;
        foreach (var (symbol, declarations) in syntaxContextReceiver.RegistrationTypes)
        {

            IEnumerable<INamedTypeSymbol> processedTypes
                = TypesToRegister(
                    GetDeclarationOverriddenRegisterMethod(declarations, registrarTypeSymbol),
                    registrarNonFactoryMethods);
            string factories = string.Join(Environment.NewLine, processedTypes.Select(t => $"\t\t\t{GenerateTypeFactory(t)}"));
            string source = @$"namespace {symbol.ContainingNamespace.GetFullNamespaceName().RemovePrefix(Consts.global)}
{{
    partial class {symbol.Name}
    {{
        protected override void DictateServiceFactories(IThunderboltFactoryDictator dictator)
        {{
{factories}
        }}
    }}
}}";
            context.AddSource($"{symbol.Name}.generated.cs", source);
        }
    }
}

