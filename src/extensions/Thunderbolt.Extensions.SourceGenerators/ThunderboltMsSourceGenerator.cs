using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Newtonsoft.Json;

using System.Diagnostics;

using Thunderbolt.GeneratorAbstractions;

namespace Thunderbolt.Extensions.SourceGenerators;

[Generator]
public class ThunderboltMsSourceGenerator : ISourceGenerator
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

        if (context.ParseOptions is not CSharpParseOptions parseOptions || context.Compilation is not CSharpCompilation compilation || compilation.Options is not CSharpCompilationOptions compilationOptions)
        {
            return;
        }
        //the following must be true for the generator to work anyway,
        //  however, I'm only keeping it here should I ever need it
        //bool above9 = parseOptions.LanguageVersion >= LanguageVersion.CSharp9;
        //-----

        GeneratorConfig generatorConfig;
        if (context.AdditionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path).ToLowerInvariant() == "thunderbolt.config.json") is AdditionalText configFile
            && configFile.GetText()?.ToString() is string configText
            && !string.IsNullOrWhiteSpace(configText))
        {
            generatorConfig = JsonConvert.DeserializeObject<GeneratorConfig>(configText);
        }
        else
        {
            generatorConfig = default;
        }

        compilationOptions = compilationOptions.WithMetadataImportOptions(MetadataImportOptions.All);
        compilation = compilation.WithOptions(compilationOptions);

        INamedTypeSymbol? registrarTypeSymbol = Util.GetRegistrarTypeSymbol(compilation);
        HashSet<IMethodSymbol>? registrarNonFactoryMethods = Util.GetRegistrarNonFactoryMethods(registrarTypeSymbol);
        if (registrarTypeSymbol is null || registrarNonFactoryMethods is null)
        {
            return;
        }
        INamedTypeSymbol registrationSymbol = compilation.GetFirstRegistration();
        string symbolFullName = registrationSymbol.GetFullyQualifiedName()!;
        string typesUtilPath = TempSourceUtil.Emit(compilation, context.AnalyzerConfigOptions.GlobalOptions, symbolFullName);
        string serializedServices = TempSourceUtil.RunTempSource(typesUtilPath, generatorConfig.StartupArgs);
        var msDescriptors
            = JsonConvert
            .DeserializeObject<ServiceDescriptor[]>(serializedServices);
        var allServices = compilation.GetAllServices(msDescriptors);
        var specialServices = Util.GetSpecialServices(compilation);
        Dictionary<TypeDescriptor, RequiredField> requiredFields = new();
        var effectiveServices = allServices.Exclude(specialServices);

        //string staticRegistrer = GeneratorHelper.GenerateStaticRegister(effectiveServices ...);
        string dictateTypeFactories = GeneratorHelper.GenerateDictateTypeFactories(effectiveServices, allServices, requiredFields);
        string requiredFieldsStr = GeneratorHelper.GenerateRequiredFields(allServices, requiredFields);

        string source =
$@"#pragma warning disable
#nullable enable

using global::System.Linq;

namespace {registrationSymbol.ContainingNamespace.GetFullNamespaceName().RemovePrefix(Consts.global)}
{{
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute(""Thunderbolt"", ""{FileVersionInfo.GetVersionInfo(GetType().Assembly.Location).ProductVersion}"")]
    partial class {registrationSymbol.Name}
    {{
        protected override void GeneratedRegistration<T>(T reg)
        {{
{(string.IsNullOrWhiteSpace(requiredFieldsStr) ? "" : requiredFieldsStr.AddIndentation(3))}
{(string.IsNullOrWhiteSpace(dictateTypeFactories) ? "" : dictateTypeFactories.AddIndentation(3))}
        }}
    }}
}}
#nullable restore
#pragma warning restore";
        //{(string.IsNullOrWhiteSpace(staticRegistrer) ? "" : staticRegistrer.AddIndentation(3))}

        context.AddSource($"{registrationSymbol.Name}.g.cs", source);
    }
}
