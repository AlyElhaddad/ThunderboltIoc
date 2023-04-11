using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;

namespace Thunderbolt.GeneratorAbstractions;

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

    internal static IEnumerable<ServiceDescriptor> TypesToRegister(Compilation compilation, IEnumerable<(MethodDeclarationSyntax methodDecl, SemanticModel semanticModel)> overriddenRegisterDeclarations, HashSet<IMethodSymbol> registrarNonFactoryMethods)
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
                var syntaxExpressionName = (syntax.Expression as MemberAccessExpressionSyntax)?.Name;
                var (genericName, identifierName) = (syntaxExpressionName as GenericNameSyntax, syntaxExpressionName as IdentifierNameSyntax);

                if (identifierName is not null
                    && op.Arguments.Length == 1
                    && op.Arguments[0].Parameter is IParameterSymbol parameterSymbol
                    && Regex.IsMatch(op.TargetMethod.OriginalDefinition.Name, @"Add(Transient|Scoped|Singleton)Factory")
                    && parameterSymbol.Type.GenericArgs() is IEnumerable<ITypeSymbol> genericArgs
                    && genericArgs.Count() == 1)
                {
                    //factory, inferred type
                    yield return new ServiceDescriptor(
                        lifetime: null,
                        serviceType: TypeDescriptor.FromTypeSymbol(genericArgs.First(), compilation),
                        implType: null,
                        implSelectorTypes: null,
                        hasFactory: true,
                        shouldUseFullDictate: false);
                    continue;
                }
                else if (genericName is null)
                {
                    continue;
                }

                if (!syntax.ArgumentList.Arguments.Any())
                {
                    TypeSyntax serviceArg = genericName.TypeArgumentList.Arguments[0];
                    if (genericName.TypeArgumentList.Arguments.Count == 1)
                    {
                        //single argument signatures are reserved for factory registrations
                        //1 or 2 generic args - a redundant/double check just to be sure
                        if (semanticModel.GetSpeculativeTypeInfo(serviceArg.SpanStart, serviceArg, SpeculativeBindingOption.BindAsTypeOrNamespace).Type is INamedTypeSymbol serviceType)
                        {
                            yield return new ServiceDescriptor(
                                lifetime: null,
                                serviceType: TypeDescriptor.FromTypeSymbol(serviceType, compilation),
                                implType: null,
                                implSelectorTypes: null,
                                hasFactory: false,
                                shouldUseFullDictate: false);
                        }
                    }
                    else if (genericName.TypeArgumentList.Arguments.Count == 2)
                    {
                        TypeSyntax implArg = genericName.TypeArgumentList.Arguments[1];
                        if (semanticModel.GetSpeculativeTypeInfo(serviceArg.SpanStart, serviceArg, SpeculativeBindingOption.BindAsTypeOrNamespace).Type is INamedTypeSymbol serviceType
                            && semanticModel.GetSpeculativeTypeInfo(implArg.SpanStart, implArg, SpeculativeBindingOption.BindAsTypeOrNamespace).Type is INamedTypeSymbol implType)
                        {
                            yield return new ServiceDescriptor(
                                lifetime: null,
                                serviceType: TypeDescriptor.FromTypeSymbol(serviceType, compilation),
                                implType: TypeDescriptor.FromTypeSymbol(implType, compilation),
                                implSelectorTypes: null,
                                hasFactory: false,
                                shouldUseFullDictate: false);
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
                        yield return new ServiceDescriptor(
                            lifetime: null,
                            serviceType: TypeDescriptor.FromTypeSymbol(serviceType, compilation),
                            implType: null,
                            implSelectorTypes: null,
                            hasFactory: true,
                            shouldUseFullDictate: false);
                        continue;
                    }

                    if (syntax.ArgumentList.Arguments.Count == 0)
                        continue;

                    //implSelector signature
                    ArgumentSyntax arg = syntax.ArgumentList.Arguments[0];
                    IEnumerable<TypeOfExpressionSyntax>? typeofStatements = null;
                    if (arg.Expression is ParenthesizedLambdaExpressionSyntax lambdaExpr)
                    { // () => { }
                        typeofStatements
                            = lambdaExpr.Block?.DescendantNodes()?.OfType<ReturnStatementSyntax>()?.Select(ret => ret.Expression)?.OfType<TypeOfExpressionSyntax>()
                            ?? (lambdaExpr.ExpressionBody is TypeOfExpressionSyntax typeofExpr ? typeofExpr : null)?.AsEnumerable()
                            .NullIfEmpty();
                    }
                    else if (arg.Expression is AnonymousMethodExpressionSyntax anonymousExpr)
                    { // delegate { }
                        typeofStatements
                            = anonymousExpr.Block?.DescendantNodes().OfType<ReturnStatementSyntax>().Select(ret => ret.Expression).OfType<TypeOfExpressionSyntax>().NullIfEmpty();
                    }
                    else
                    {
                        //undocumented syntax, it's oaky to ignore it.
                        continue;
                    }
                    yield return new ServiceDescriptor(
                        lifetime: null,
                            serviceType: TypeDescriptor.FromTypeSymbol(serviceType, compilation),
                        implType: null,
                        implSelectorTypes:
                            typeofStatements
                                .Select(typeOfExpr => semanticModel.GetSpeculativeTypeInfo(typeOfExpr.Type.SpanStart, typeOfExpr.Type, SpeculativeBindingOption.BindAsTypeOrNamespace).Type)
                                .OfType<INamedTypeSymbol>()
                                .Select(serviceType => TypeDescriptor.FromTypeSymbol(serviceType, compilation)),
                        hasFactory: false,
                        shouldUseFullDictate: false);
                }
            }
        }
    }
}

