using Microsoft.CodeAnalysis;

namespace ThunderboltIoc.SourceGenerators;

[Generator]
public class ContainerSourceGenerator : ISourceGenerator
{

    public void Initialize(GeneratorInitializationContext context)
    {
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
