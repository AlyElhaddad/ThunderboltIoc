using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

using System.Text;

namespace ThunderboltIoc.SourceGenerators;

internal static class ExplicitGeneratorHelper
{
    internal static IEnumerable<(MethodDeclarationSyntax methodDecl, SemanticModel semanticModel)> GetDeclarationOverriddenRegisterMethod(IEnumerable<(ClassDeclarationSyntax declaration, SemanticModel semanticModel)> symbolDeclarations, INamedTypeSymbol registrarTypeSymbol)
    {
        return
            symbolDeclarations
            .SelectMany(item => item.declaration.GetDeclaredMethods(Consts.RegisterMethodName, Consts.@protected, Consts.@override).Select(m => (methodDecl: m, item.semanticModel)))
            .Where(item => item.semanticModel.GetDeclaredSymbol(item.methodDecl) is IMethodSymbol method
                && !method.TypeParameters.Any()
                && method.Parameters.Length == 1
                && method.Parameters[0].Type.GetFullyQualifiedName() == registrarTypeSymbol.GetFullyQualifiedName());
        //SymbolEqualityComparer.Default.Equals turned out to be bugged and unreliable
        //  it returns trues and falses indeterministicly for the same unchanged code-base
        //  I have therefore decided to make the last comparison by types' full names as strings instead.
    }

    internal static IEnumerable<(INamedTypeSymbol service, INamedTypeSymbol? impl, IEnumerable<INamedTypeSymbol>? selectorImpls, bool hasFactory)> TypesToRegister(IEnumerable<(MethodDeclarationSyntax methodDecl, SemanticModel semanticModel)> overriddenRegisterDeclarations, HashSet<IMethodSymbol> registrarNonFactoryMethods)
    {
        foreach (var (registerDecl, semanticModel) in overriddenRegisterDeclarations)
        {
            BlockSyntax blockStatement = registerDecl.Body ?? throw new MissingMemberException();
#pragma warning disable RS1024 // Symbols should be compared for equality
            IEnumerable<(InvocationExpressionSyntax syntax, IInvocationOperation op)> invExpressions
                = blockStatement.DescendantNodes().OfType<InvocationExpressionSyntax>()
                    .Select(inv => (inv, invOp: semanticModel.GetOperation(inv) as IInvocationOperation))
                    .OfType<(InvocationExpressionSyntax inv, IInvocationOperation invOp)>()
                    .Where(item => registrarNonFactoryMethods.Contains(item.invOp.TargetMethod.OriginalDefinition, MethodDefinitionEqualityComparer.Default));
            // Just like INamedTypeSymbol, SymbolEqualityComparer does not seem to be reliable when it comes to comparing IMethodSymbol
#pragma warning restore RS1024 // Symbols should be compared for equality
            foreach (var (syntax, op) in invExpressions)
            {
                if ((syntax.Expression as MemberAccessExpressionSyntax)?.Name is not GenericNameSyntax genericName)
                    continue;
                if (!syntax.ArgumentList.Arguments.Any())
                {
                    TypeSyntax serviceArg = genericName.TypeArgumentList.Arguments[0];
                    if (genericName.TypeArgumentList.Arguments.Count == 1)
                    {
                        //single argument signatures are reserved for factory registrations
                        //1 or 2 generic args - a redundant/double check just to be sure
                        if (semanticModel.GetSpeculativeTypeInfo(serviceArg.SpanStart, serviceArg, SpeculativeBindingOption.BindAsTypeOrNamespace).Type is INamedTypeSymbol serviceType)
                        {
                            yield return (serviceType, null, null, false);
                        }
                    }
                    else if (genericName.TypeArgumentList.Arguments.Count == 2)
                    {
                        TypeSyntax implArg = genericName.TypeArgumentList.Arguments[1];
                        if (semanticModel.GetSpeculativeTypeInfo(serviceArg.SpanStart, serviceArg, SpeculativeBindingOption.BindAsTypeOrNamespace).Type is INamedTypeSymbol serviceType
                            && semanticModel.GetSpeculativeTypeInfo(implArg.SpanStart, implArg, SpeculativeBindingOption.BindAsTypeOrNamespace).Type is INamedTypeSymbol implType)
                        {
                            yield return (serviceType, implType, null, false);
                        }
                    }
                }
                else if (syntax.ArgumentList.Arguments.Count == 1 && genericName.TypeArgumentList.Arguments.Count == 1)
                {
                    TypeSyntax serviceArg = genericName.TypeArgumentList.Arguments[0];
                    if (semanticModel.GetSpeculativeTypeInfo(serviceArg.SpanStart, serviceArg, SpeculativeBindingOption.BindAsTypeOrNamespace).Type is not INamedTypeSymbol serviceType)
                        continue;
                    if (op.TargetMethod.OriginalDefinition.Name.EndsWith(Consts.FactorySuffix))
                    {
                        //factory
                        yield return (serviceType, null, null, true);
                        continue;
                    }
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
                    yield return
                        (serviceType,
                        null,
                        implSelectorBlockStatement
                            .DescendantNodes()
                            .OfType<ReturnStatementSyntax>()
                            .Select(ret => ret.Expression)
                            .OfType<TypeOfExpressionSyntax>()
                            .Select(typeOfExpr => semanticModel.GetSpeculativeTypeInfo(typeOfExpr.Type.SpanStart, typeOfExpr.Type, SpeculativeBindingOption.BindAsTypeOrNamespace).Type)
                            .OfType<INamedTypeSymbol>(),
                        false);
                }
            }
        }
    }
}

