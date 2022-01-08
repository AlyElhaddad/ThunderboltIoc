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
        //        //this is for the purpose of debugging the source generator itself and can be ignored
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

        if (RegistrarTypeSymbol is null || RegistrarNonFactoryMethods is null)
        {
            RegistrarTypeSymbol = context.Compilation.GetTypeByFullName(Consts.IIocRegistrarTypeFullName);
#pragma warning disable RS1024 // Symbols should be compared for equality
            RegistrarNonFactoryMethods = new(RegistrarTypeSymbol?.GetMembers().OfType<IMethodSymbol>().Where(m => !m.Name.EndsWith(Consts.FactorySuffix)).Select(m => m.OriginalDefinition) ?? Enumerable.Empty<IMethodSymbol>(), MethodDefinitionEqualityComparer.Default);
#pragma warning restore RS1024 // Symbols should be compared for equality
            if (RegistrarTypeSymbol is null || RegistrarNonFactoryMethods is null)
            {
                return;
            }
        }

        AttributeGeneratorHelpers.GenerateRegisterStaticTypes(context);
        FactoryGeneratorHelpers.GenerateAddFactoriesForRegisteredTypes(context, RegistrarTypeSymbol, RegistrarNonFactoryMethods);
    }
}
