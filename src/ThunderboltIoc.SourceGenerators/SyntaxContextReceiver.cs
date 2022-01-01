using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace ThunderboltIoc.SourceGenerators;

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
            && invExp.Expression is MemberAccessExpressionSyntax memberExp
            && memberExp.Name is GenericNameSyntax genericName
            && genericName.Identifier.ValueText == Consts.AttachMethodName
            && genericName.TypeArgumentList.Arguments.Count == 1
            && context.SemanticModel.GetOperation(invExp) is IInvocationOperation invOp
            && invOp.TargetMethod.Name == Consts.AttachMethodName //use the semantic model to make sure it's our method, not some other method called attach
            && invOp.TargetMethod.ContainingType.GetFullyQualifiedName() == Consts.ActivatorTypeFullName)
        {
            //get our type, and then make sure it is declared in the current assembly and that it's a partial class
            //the fact that it must be a ThunderboltRegistration is already implied by the generic param constraint of the attach method
            TypeSyntax typeSyntax = genericName.TypeArgumentList.Arguments[0];
            if (context.SemanticModel.GetSpeculativeTypeInfo(typeSyntax.SpanStart, typeSyntax, SpeculativeBindingOption.BindAsTypeOrNamespace).Type is INamedTypeSymbol regType
                && SymbolEqualityComparer.Default.Equals(regType.ContainingAssembly, context.SemanticModel.Compilation.Assembly))
            {
                IEnumerable<(ClassDeclarationSyntax decl, SemanticModel semantic)> declarations
                    = regType.DeclaringSyntaxReferences
                    .SelectMany(sr => sr.SyntaxTree.GetRoot().DescendantNodesAndSelf(sr.Span).OfType<ClassDeclarationSyntax>().Select(decl => (decl, semanticModel: context.SemanticModel.Compilation.GetSemanticModel(sr.SyntaxTree))))
                    .Where(item => item.decl.Identifier.ValueText == regType.Name);
                if (declarations.Any() && declarations.All(item => item.decl.Modifiers.Any(m => m.ValueText == Consts.partial)))
                {
                    registrationTypes.Add((regType, declarations));
                }
            }
        }
    }
}
