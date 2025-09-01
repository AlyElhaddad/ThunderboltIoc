using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Newtonsoft.Json;

using System.Diagnostics;

using Thunderbolt.GeneratorAbstractions;

namespace ThunderboltIoc.SourceGenerators;

#pragma warning disable RS1036 // Specify analyzer banned API enforcement setting
#pragma warning disable RS1042 // Implementations of this interface are not allowed
[Generator]
public class ThunderboltSourceGenerator : ISourceGenerator
#pragma warning restore RS1042 // Implementations of this interface are not allowed
#pragma warning restore RS1036 // Specify analyzer banned API enforcement setting
{
    public virtual void Initialize(GeneratorInitializationContext context)
    {
        //this is for the purpose of debugging the source generator itself and can be ignored
        //#if DEBUG
        //        if (!System.Diagnostics.Debugger.IsAttached)
        //        {
        //            System.Diagnostics.Debugger.Launch();
        //        }
        //#endif
    }

    public virtual void Execute(GeneratorExecutionContext context)
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
        GeneratorConfig.Instance = generatorConfig;


        compilationOptions = compilationOptions.WithMetadataImportOptions(MetadataImportOptions.All);
        compilation = compilation.WithOptions(compilationOptions);

        INamedTypeSymbol? registrarTypeSymbol = Util.GetRegistrarTypeSymbol(compilation);
        HashSet<IMethodSymbol>? registrarNonFactoryMethods = Util.GetRegistrarNonFactoryMethods(registrarTypeSymbol);
        if (registrarTypeSymbol is null || registrarNonFactoryMethods is null)
        {
            return;
        }

        //the following must be true for the generator to work anyway,
        //  however, I'm only keeping it here should I ever need it
        //bool above9 = parseOptions.LanguageVersion >= LanguageVersion.CSharp9;
        //-----

        var allServices //of all registrations
            = compilation.GetAllServicesWithSymbols(out var symbols); //symbols being registration classes
        var specialServices = Util.GetSpecialServices(compilation);
        foreach (INamedTypeSymbol symbol in symbols)
        {
            string symbolFullName = symbol.GetFullyQualifiedName()!;
            var effectiveServicePairs
                = allServices
                .Where(item => !specialServices.Contains(item.service) && (item.symbol is null || item.symbol.GetFullyQualifiedName() == symbolFullName));
            var effectiveServices
                = effectiveServicePairs
                .Select(item => item.service);
            Dictionary<TypeDescriptor, IDictionary<int, RequiredField>> requiredFields = new();
            //string staticRegistrer = GeneratorHelper.GenerateStaticRegister(effectiveServicePairs.Where(s => s.service.ServiceType.IsNonClosedGenericType || s.service.ImplType?.IsNonClosedGenericType == true), requiredFields);
            string dictateTypeFactories = GeneratorHelper.GenerateDictateTypeFactories(effectiveServices, new HashSet<ServiceDescriptor>(allServices.Select(item => item.service)), requiredFields);
            string requiredFieldsStr = GeneratorHelper.GenerateRequiredFields(effectiveServices, requiredFields);

            string source =
$@"#pragma warning disable
#nullable enable

using global::System.Linq;

namespace {symbol.ContainingNamespace.GetFullNamespaceName().RemovePrefix(Consts.global)}
{{
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute(""ThunderboltIoc"", ""{FileVersionInfo.GetVersionInfo(GetType().Assembly.Location).ProductVersion}"")]
    partial class {symbol.Name}
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

            context.AddSource($"{symbol.Name}.g.cs", source);
        }
    }
}
