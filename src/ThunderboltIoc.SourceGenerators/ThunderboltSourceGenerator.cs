using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using System.Diagnostics;

namespace ThunderboltIoc.SourceGenerators;

[Generator]
public class ThunderboltSourceGenerator : ISourceGenerator
{
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

#pragma warning disable RS1024 // Symbols should be compared for equality
        INamedTypeSymbol? registrarTypeSymbol = context.Compilation.GetTypeByFullName(Consts.IIocRegistrarTypeFullName);
        HashSet<IMethodSymbol>? registrarNonFactoryMethods = new(registrarTypeSymbol?.GetMembers().OfType<IMethodSymbol>().Select(m => m.OriginalDefinition) ?? Enumerable.Empty<IMethodSymbol>(), MethodDefinitionEqualityComparer.Default);
#pragma warning restore RS1024 // Symbols should be compared for equality
        if (registrarTypeSymbol is null || registrarNonFactoryMethods is null || context.ParseOptions is not CSharpParseOptions parseOptions)
        {
            return;
        }
        //bool above72 = parseOptions.LanguageVersion >= LanguageVersion.CSharp7_2;

        if (context.SyntaxContextReceiver is not SyntaxContextReceiver syntaxContextReceiver)
            return;

        var attributeRegistration = AttributeGeneratorHelper.AllIncludedTypes(context.Compilation);
        foreach (var (symbol, declarations) in syntaxContextReceiver.RegistrationTypes)
        {
            var explicitRegistration
                = ExplicitGeneratorHelper.TypesToRegister(
                    ExplicitGeneratorHelper.GetDeclarationOverriddenRegisterMethod(declarations, registrarTypeSymbol),
                    registrarNonFactoryMethods);

            var allTypes
                = attributeRegistration
                    .WhereIf(explicitRegistration.Any(), attrReg => !explicitRegistration.Any(explReg => attrReg.ServiceSymbol.GetFullyQualifiedName() == explReg.ServiceSymbol.GetFullyQualifiedName()))
                    .Concat(
                        explicitRegistration);

            if (!allTypes.Any())
                continue;

            string staticRegistrer = GeneratorHelper.GenerateStaticRegister(allTypes);
            string dictateServiceFactories = GeneratorHelper.GenerateDictateServiceFactories(allTypes);

            string source =
$@"namespace {symbol.ContainingNamespace.GetFullNamespaceName().RemovePrefix(Consts.global)}
{{
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute(""ThunderboltIoc"", ""{FileVersionInfo.GetVersionInfo(GetType().Assembly.Location).ProductVersion}"")]
    partial class {symbol.Name}
    {{
{(string.IsNullOrWhiteSpace(staticRegistrer) ? "" : staticRegistrer.AddIndentation(2))}
{(string.IsNullOrWhiteSpace(dictateServiceFactories) ? "" : dictateServiceFactories.AddIndentation(2))}
    }}
}}";
            context.AddSource($"{symbol.Name}.generated.cs", source);
        }
    }
}
