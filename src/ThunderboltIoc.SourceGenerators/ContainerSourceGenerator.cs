using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ThunderboltIoc.SourceGenerators;

[Generator]
public class ContainerSourceGenerator : ISourceGenerator
{
    internal static INamedTypeSymbol? RegistrarTypeSymbol { get; private set; }
    internal static HashSet<IMethodSymbol>? RegistrarNonFactoryMethods { get; private set; }

    public void Initialize(GeneratorInitializationContext context)
    {
        //this is for the purpose of debugging the source generator itself and can be ignored
//#if DEBUG
//        if (!System.Diagnostics.Debugger.IsAttached)
//        {
//            System.Diagnostics.Debugger.Launch();
//        }
//#endif
        context.RegisterForSyntaxNotifications(() => new SyntaxContextReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        //        //this is for the purpose of debugging the source generator itself and can be ignored
        //#if DEBUG
        //        if (!System.Diagnostics.Debugger.IsAttached)
        //        {
        //            System.Diagnostics.Debugger.Launch();
        //        }
        //#endif

        if (RegistrarTypeSymbol is null)
        {
            RegistrarTypeSymbol = context.Compilation.GetTypeByFullName(Consts.IIocRegistrarTypeFullName);
            RegistrarNonFactoryMethods = new(RegistrarTypeSymbol?.GetMembers().OfType<IMethodSymbol>().Where(m => !m.Name.EndsWith(Consts.FactorySuffix)), SymbolEqualityComparer.Default);
        }

        //AttributeGeneratorHelpers.GenerateRegisterStaticTypes(context);
        FactoryGeneratorHelpers.GenerateAddFactoriesForRegisteredTypes(context, RegistrarTypeSymbol, RegistrarNonFactoryMethods);
    }
}
