using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using System.Diagnostics;
using System.Xml.Linq;

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

        INamedTypeSymbol? registrarTypeSymbol = GetRegistrarTypeSymbol(context.Compilation);
        HashSet<IMethodSymbol>? registrarNonFactoryMethods = GetRegistrarNonFactoryMethods(registrarTypeSymbol);
        if (registrarTypeSymbol is null || registrarNonFactoryMethods is null || context.ParseOptions is not CSharpParseOptions parseOptions)
        {
            return;
        }
        //this must be true for the generator to work anyway,
        //  however, I'm only keeping it here should I ever need it
        //bool above9 = parseOptions.LanguageVersion >= LanguageVersion.CSharp9;

        if (context.SyntaxContextReceiver is not SyntaxContextReceiver syntaxContextReceiver)
            return;

        var attributeRegistration = AttributeGeneratorHelper.AllIncludedTypes(context.Compilation);

        var allExplicitRegistrations
            = syntaxContextReceiver
            .RegistrationTypes
            .SelectMany(type => ExplicitGeneratorHelper.TypesToRegister(
                                    ExplicitGeneratorHelper.GetDeclarationOverriddenRegisterMethod(type.declarations, registrarTypeSymbol),
                                    registrarNonFactoryMethods))
            .Select(descriptor => descriptor.ServiceSymbol);
        var allServices
            = attributeRegistration
            .Select(descriptor => descriptor.ServiceSymbol)
            .WhereIf(allExplicitRegistrations.Any(), attrReg => !allExplicitRegistrations.Any(explReg => attrReg.GetFullyQualifiedName() == explReg.GetFullyQualifiedName()))
            .Concat(allExplicitRegistrations);

        foreach (var (symbol, declarations) in syntaxContextReceiver.RegistrationTypes)
        {
            var explicitRegistration
                = ExplicitGeneratorHelper.TypesToRegister(
                    ExplicitGeneratorHelper.GetDeclarationOverriddenRegisterMethod(declarations, registrarTypeSymbol),
                    registrarNonFactoryMethods);

            var allTypes
                = attributeRegistration
                    .WhereIf(explicitRegistration.Any(), attrReg => !explicitRegistration.Any(explReg => attrReg.ServiceSymbol.GetFullyQualifiedName() == explReg.ServiceSymbol.GetFullyQualifiedName()))
                    .Concat(explicitRegistration);

            if (!allTypes.Any())
                continue;

            string staticRegistrer = GeneratorHelper.GenerateStaticRegister(allTypes);
            string dictateServiceFactories = GeneratorHelper.GenerateDictateServiceFactories(allTypes, allServices);

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

#pragma warning disable RS1024 // Symbols should be compared for equality
    internal static INamedTypeSymbol? GetRegistrarTypeSymbol(Compilation compilation)
    {
        return compilation.GetTypeByFullName(Consts.IIocRegistrarTypeFullName);
    }
    internal static HashSet<IMethodSymbol> GetRegistrarNonFactoryMethods(INamedTypeSymbol? registrarTypeSymbol)
    {
        return new (registrarTypeSymbol?.GetMembers().OfType<IMethodSymbol>().Select(m => m.OriginalDefinition) ?? Enumerable.Empty<IMethodSymbol>(), MethodDefinitionEqualityComparer.Default);
#pragma warning restore RS1024 // Symbols should be compared for equality
    }
}
