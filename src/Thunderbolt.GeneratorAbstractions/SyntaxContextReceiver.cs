using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Thunderbolt.GeneratorAbstractions;

public class SyntaxContextReceiver : ISyntaxContextReceiver
{
    internal SyntaxContextReceiver()
    {
        registrationTypes = new();
    }

    private readonly List<(INamedTypeSymbol symbol, IEnumerable<(ClassDeclarationSyntax, SemanticModel)> declarations)> registrationTypes;
    public IReadOnlyList<(INamedTypeSymbol symbol, IEnumerable<(ClassDeclarationSyntax declaration, SemanticModel semanticModel)> declarations)> RegistrationTypes => registrationTypes;

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is InvocationExpressionSyntax invExp //start by hooking calls to attach method
            && GetRegisteredType(invExp, context.SemanticModel) is (INamedTypeSymbol symbol, IEnumerable<(ClassDeclarationSyntax, SemanticModel)> declarations)
            && symbol.GetFullyQualifiedName() is string symbolTypeFullName
            && !registrationTypes.Any(item => symbolTypeFullName.Equals(item.symbol.GetFullyQualifiedName())))
        {
            registrationTypes.Add((symbol, declarations));
        }
    }

    internal static (INamedTypeSymbol symbol, IEnumerable<(ClassDeclarationSyntax declaration, SemanticModel semanticModel)> declarations) GetRegisteredType(InvocationExpressionSyntax invExp, SemanticModel semanticModel)
    {
        if (invExp.Expression is MemberAccessExpressionSyntax memberExp
            && memberExp.Name is GenericNameSyntax genericName
            && genericName.Identifier.ValueText is Consts.AttachMethodName or Consts.UseMethodName //or .UseThunderbolt
            && genericName.TypeArgumentList.Arguments.Count == 1
            && semanticModel.GetOperation(invExp) is IInvocationOperation invOp
            && invOp.TargetMethod.Name is Consts.AttachMethodName or Consts.UseMethodName //use the semantic model to make sure it's our method, not some other method called attach
            && invOp.TargetMethod.ContainingType.GetFullyQualifiedName() is Consts.ActivatorTypeFullName or Consts.ExtensionsTypeFullName) //or ThunderboltExtensions
        {
            //get our type, and then make sure it is declared in the current assembly and that it's a partial class
            //the fact that it must be a ThunderboltRegistration is already implied by the generic param constraint of the attach method
            TypeSyntax typeSyntax = genericName.TypeArgumentList.Arguments[0];
            if (semanticModel.GetSpeculativeTypeInfo(typeSyntax.SpanStart, typeSyntax, SpeculativeBindingOption.BindAsTypeOrNamespace).Type is INamedTypeSymbol regType
                && SymbolEqualityComparer.Default.Equals(regType.ContainingAssembly, semanticModel.Compilation.Assembly))
            {
                IEnumerable<(ClassDeclarationSyntax decl, SemanticModel semantic)> declarations
                    = regType.DeclaringSyntaxReferences
                    .SelectMany(sr => sr.SyntaxTree.GetRoot().DescendantNodesAndSelf(sr.Span).OfType<ClassDeclarationSyntax>().Select(decl => (decl, semanticModel: semanticModel.Compilation.GetSemanticModel(sr.SyntaxTree))))
                    .Where(item => item.decl.Identifier.ValueText == regType.Name);
                if (declarations.Any()
                    && declarations.All(item => item.decl.Modifiers.Any(m => m.ValueText == Consts.partial)))
                {
                    return (regType, declarations);
                }
            }
        }
        return default;
    }
}
