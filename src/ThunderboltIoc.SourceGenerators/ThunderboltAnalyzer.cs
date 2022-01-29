using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

using System.Collections.Immutable;

namespace ThunderboltIoc.SourceGenerators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ThunderboltAnalyzer : DiagnosticAnalyzer
{
    private static readonly IEnumerable<string> thunderboltAttributeNames = new string[]
    {
        Consts.includeAttrName,
        Consts.excludeAttrName,
        Consts.regexIncludeAttrName,
        Consts.regexExcludeAttrName
    };
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => supportedDiagnostics;

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterSyntaxNodeAction(RegistrationClassHandler, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(ThunderboltAttributeHandler, SyntaxKind.Attribute);
        context.RegisterCompilationAction(ThunderboltCompilationHandler);
    }
    private static void ThunderboltCompilationHandler(CompilationAnalysisContext context)
    {
        try
        {
            INamedTypeSymbol? registrarTypeSymbol = ThunderboltSourceGenerator.GetRegistrarTypeSymbol(context.Compilation);
            HashSet<IMethodSymbol>? registrarNonFactoryMethods = ThunderboltSourceGenerator.GetRegistrarNonFactoryMethods(registrarTypeSymbol);
            if (registrarTypeSymbol is null || registrarNonFactoryMethods is null)
            {
                return;
            }

            #region CyclicDependencies && NoSuitableConstructor
            //Get attribute registrations
            var attributeReg = AttributeGeneratorHelper.AllIncludedTypes(context.Compilation);

            //Get explicit registrations
            var explicitReg
                = context
                .Compilation
                .SyntaxTrees
                .SelectMany(tree =>
                {
#pragma warning disable RS1030 // Do not invoke Compilation.GetSemanticModel() method within a diagnostic analyzer
                //it is advised against using GetSemanticModel within a diagnostic analyzer to prevent using it too often
                // (e.g after completing a syntax node analysis) because that would result in it being too heavy.
                // however, here we're using it only at the compilation time, times the syntax trees available, which should be okay.
                SemanticModel semanticModel = context.Compilation.GetSemanticModel(tree);
#pragma warning restore RS1030 // Do not invoke Compilation.GetSemanticModel() method within a diagnostic analyzer
                return
                    tree
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Select(invExp => SyntaxContextReceiver.GetRegisteredType(invExp, semanticModel).declarations)
                    .Where(declarations => declarations is not null)
                    .SelectMany(declarations =>
                        ExplicitGeneratorHelper.TypesToRegister(
                                ExplicitGeneratorHelper.GetDeclarationOverriddenRegisterMethod(declarations, registrarTypeSymbol),
                                registrarNonFactoryMethods));
                });

            //Get filtered final registrations
            var allRegs
                = attributeReg
                    .WhereIf(explicitReg.Any(), attrReg => !explicitReg.Any(explReg => attrReg.ServiceSymbol.GetFullyQualifiedName() == explReg.ServiceSymbol.GetFullyQualifiedName()))
                    .Concat(explicitReg);

            try
            {
                if (DependencyHelper.HasCyclicDependencies(allRegs, out var deps))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.CyclicDependencies, null, string.Join(", ", deps.Select(dep => $"'{dep.RemovePrefix(Consts.global)}'"))));
                }
            }
            catch (MissingMethodException mme) when (mme.Message?.StartsWith("Could not find a suitable constructor for type") == true)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.NoSuitableConstructor, null, mme.Message));
            }
            #endregion
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.AnalyzerException, null, $"{ex}{Environment.NewLine}{ex.StackTrace}"));
        }
    }

    private static void ThunderboltAttributeHandler(SyntaxNodeAnalysisContext context)
    {
        try
        {
            if (context.Node is not AttributeSyntax attribute)
            {
                return;
            }
            TypeSyntax attrTypeSyntax = SyntaxFactory.ParseTypeName(attribute.Name.ToString().WithSuffix("Attribute"));
            if (context.SemanticModel.GetSpeculativeTypeInfo(attribute.Name.SpanStart, attrTypeSyntax, SpeculativeBindingOption.BindAsTypeOrNamespace).Type is not INamedTypeSymbol attrSymbol
                || !thunderboltAttributeNames.Contains(attrSymbol.GetFullyQualifiedName()))
            {
                return;
            }

            #region MissingRegistration
            if (!context
                .Compilation
                .SyntaxTrees
                .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                .SelectMany(classDecl => classDecl.BaseList is null ? Enumerable.Empty<INamedTypeSymbol>() : classDecl.BaseList.Types.Select(t => context.SemanticModel.GetSpeculativeTypeInfo(t.Type.SpanStart, t.Type, SpeculativeBindingOption.BindAsTypeOrNamespace).Type).OfType<INamedTypeSymbol>())
                .Any(symbol => symbol.GetFullyQualifiedName() == Consts.RegistrationTypeFullName))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.MissingRegistration, attribute.Name.GetLocation()));
            }
            #endregion
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.AnalyzerException, null, $"{ex}{Environment.NewLine}{ex.StackTrace}"));
        }
    }

    private static void RegistrationClassHandler(SyntaxNodeAnalysisContext context)
    {
        try
        {
#pragma warning disable CS8604 // Possible null reference argument.
            if (context.Node is not ClassDeclarationSyntax classDeclaration
                || context.ContainingSymbol is not INamedTypeSymbol namedTypeSymbol
                || !namedTypeSymbol.HasParent(context.SemanticModel.GetTypeByFullName(Consts.RegistrationTypeFullName)))
            {
                return;
            }
#pragma warning restore CS8604 // Possible null reference argument.

            string fullName = namedTypeSymbol.GetFullyQualifiedName();
            string name = fullName.RemovePrefix(Consts.global);
            Location location = classDeclaration.Identifier.GetLocation();

            Parallel.Invoke(
            #region RegistrationNotAttached
        () =>
        {
            try
            {
                if (!context
                    .Compilation
                    .SyntaxTrees
                    .SelectMany(t =>
                        t.GetRoot()
                        .DescendantNodes()
                        .OfType<InvocationExpressionSyntax>())
                    .Any(invExp => invExp.Expression is MemberAccessExpressionSyntax memberExp
                        && memberExp.Name is GenericNameSyntax genericName
                        && genericName.Identifier.ValueText == Consts.AttachMethodName
                        && genericName.TypeArgumentList.Arguments.Count == 1
                        && context.SemanticModel.GetOperation(invExp) is IInvocationOperation invOp
                        && invOp.TargetMethod.Name == Consts.AttachMethodName
                        && invOp.TargetMethod.ContainingType.GetFullyQualifiedName() == Consts.ActivatorTypeFullName
                        && genericName.TypeArgumentList.Arguments.First() is TypeSyntax typeSyntax
                        && context.SemanticModel.GetSpeculativeTypeInfo(typeSyntax.SpanStart, typeSyntax, SpeculativeBindingOption.BindAsTypeOrNamespace).Type is INamedTypeSymbol regType
                        && regType.GetFullyQualifiedName() == fullName))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.RegistrationNotAttached, location, name));
                }
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.AnalyzerException, null, $"{ex}{Environment.NewLine}{ex.StackTrace}"));
            }
        },
            #endregion
            #region MissingPartialModifier
        () =>
        {
            try
            {
                if (!namedTypeSymbol
                    .DeclaringSyntaxReferences
                    .Select(declRef => declRef.GetSyntax())
                    .OfType<ClassDeclarationSyntax>()
                    .SelectMany(refDeclaration => refDeclaration.Modifiers.Select(modifier => modifier.Text))
                    .Any(modifier => modifier == Consts.partial))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptors.MissingPartialModifier, location, name));
                }
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.AnalyzerException, null, $"{ex}{Environment.NewLine}{ex.StackTrace}"));
            }
        }
        #endregion
            );

        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.AnalyzerException, null, $"{ex}{Environment.NewLine}{ex.StackTrace}"));
        }
    }

    #region Diagnostics Definitions
    private static readonly ImmutableArray<DiagnosticDescriptor> supportedDiagnostics
        = ImmutableArray.Create(
            Descriptors.AnalyzerException,
            Descriptors.RegistrationNotAttached,
            Descriptors.MissingPartialModifier,
            Descriptors.MissingRegistration,
            Descriptors.GenerationFailureForType,
            Descriptors.NoSuitableConstructor,
            Descriptors.CyclicDependencies
    );
    private static class Descriptors
    {
        //TB: Thunderbolt
        //3-digit number:
        //  1: severity
        //      0: Message,
        //      1: Warning,
        //      2: Error
        //  2,3: 2-digit unique number regardless of the severity
        internal static DiagnosticDescriptor AnalyzerException
            = new("TB000",
                nameof(AnalyzerException),
                "Thunderbolt AnalyzerError: {0}",
                "AnalyzerError",
                DiagnosticSeverity.Info,
                isEnabledByDefault: true);

        internal static DiagnosticDescriptor RegistrationNotAttached
            = new("TB001",
                nameof(RegistrationNotAttached),
                $"The class '{{0}}' implements '{Consts.registrationClass}' but a call to '{Consts.activatorClass}.{Consts.RegisterMethodName}<{{0}}>()' was not found in this project. Corresponding code-generation will therefore not happen.",
                "Design",
                DiagnosticSeverity.Info,
                isEnabledByDefault: true);

        internal static DiagnosticDescriptor MissingPartialModifier
            = new("TB102",
                nameof(MissingPartialModifier),
                $"The class '{{0}}' implements '{Consts.registrationClass}' but does not have the 'partial' modifier on any of its definitions. Corresponding code-generation will therefore not happen.",
                "Design",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        internal static DiagnosticDescriptor MissingRegistration
            = new("TB103",
                nameof(MissingRegistration),
                $"Thunderbolt atribute registrations were found. However, no implementation of '{Consts.registrationClass}' was found in this project. Corresponding code-generation will therefore not happen.",
                "Design",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        internal static DiagnosticDescriptor GenerationFailureForType
            = new("TB104",
                nameof(GenerationFailureForType),
                $"Failed to generate code for '{{0}}'. Please report the relevant error on the GitHub repository.{Environment.NewLine}The relevant error:{Environment.NewLine}{{1}}",
                "Design",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        internal static DiagnosticDescriptor NoSuitableConstructor
            = new("TB205",
                nameof(NoSuitableConstructor),
                "{0}",
                "Design",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        internal static DiagnosticDescriptor CyclicDependencies
            = new("TB206",
                nameof(CyclicDependencies),
                $"Cyclic dependencies were found in the dependency tree(s) of the following service(s) ({{0}}). Runtime errors are expected.",
                "Design",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        //TODO: Registration class must be a top-level class
        //TODO: prevent registering interface/abstract class without specifying implementation
        //TOOD: prevent interface/abstract class implementation
        //TODO: implSelector: if a registered service was specified, resolve it instead of creating it
    }
    #endregion
}
