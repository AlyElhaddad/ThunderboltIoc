using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThunderboltIoc.SourceGenerators;

public class RegistrationSyntaxContextReceiver : ISyntaxContextReceiver
{
    public RegistrationSyntaxContextReceiver()
    {
        RegistrationTypes = new();
    }

    public List<INamedTypeSymbol> RegistrationTypes { get; }
    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is InvocationExpressionSyntax invExp
            && invExp.Expression is MemberAccessExpressionSyntax memberExp
            && memberExp.DescendantNodes().OfType<GenericNameSyntax>().LastOrDefault() is GenericNameSyntax genericName
            && genericName.Identifier.ValueText == Consts.AttachMethodName
            && genericName.TypeArgumentList.Arguments.Count == 1
            && context.SemanticModel.GetOperation(invExp) is IInvocationOperation invOp
            && invOp.TargetMethod.Name == Consts.AttachMethodName
            && invOp.TargetMethod.ContainingType.GetFullyQualifiedName() == Consts.ActivatorTypeFullName)
        {
            TypeSyntax typeSyntax = genericName.TypeArgumentList.Arguments[0];
            if (context.SemanticModel.GetSpeculativeTypeInfo(typeSyntax.SpanStart, typeSyntax, SpeculativeBindingOption.BindAsTypeOrNamespace).Type is INamedTypeSymbol regType)
            {
                RegistrationTypes.Add(regType);
            }
        }
    }
}

[Generator]
public class ContainerSourceGenerator : ISourceGenerator
{

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new RegistrationSyntaxContextReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        ////this is for the purpose of debugging the source generator itself and can be ignored
        //#if DEBUG
        //        if (!Debugger.IsAttached)
        //        {
        //            Debugger.Launch();
        //        }
        //#endif
        AttributeGeneratorHelpers.GenerateRegisterStaticTypes(context);
        FactoryGeneratorHelpers.GenerateAddFactoriesForRegisteredTypes(context);
    }
}
