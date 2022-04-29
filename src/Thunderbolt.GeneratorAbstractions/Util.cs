using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Thunderbolt.GeneratorAbstractions;

internal static class Util
{
#pragma warning disable RS1024 // Symbols should be compared for equality
    internal static INamedTypeSymbol? GetRegistrarTypeSymbol(Compilation compilation)
    {
        return compilation.GetTypeByFullName(Consts.IRegistrarTypeFullName);
    }
    internal static HashSet<IMethodSymbol> GetRegistrarNonFactoryMethods(INamedTypeSymbol? registrarTypeSymbol)
    {
        return new(registrarTypeSymbol?.GetMembers().OfType<IMethodSymbol>().Select(m => m.OriginalDefinition) ?? Enumerable.Empty<IMethodSymbol>(), MethodDefinitionEqualityComparer.Default);
    }
#pragma warning restore RS1024 // Symbols should be compared for equality
    internal static IEnumerable<ServiceDescriptor> GetSpecialServices(Compilation compilation)
    {
        //IThunderboltContainer
        yield return new ServiceDescriptor(
            Consts.SingletonValue,
            TypeDescriptor.FromTypeSymbol(compilation.GetTypeByFullName(Consts.IContainerTypeFullName)!, compilation),
            null,
            null,
            true,
            false);

        //IThunderboltScope
        yield return new ServiceDescriptor(
            Consts.TransientValue,
            TypeDescriptor.FromTypeSymbol(compilation.GetTypeByFullName(Consts.IScopeTypeFullName)!, compilation),
            null,
            null,
            true,
            false);

        //IThunderboltResolver
        yield return new ServiceDescriptor(
            Consts.TransientValue,
            TypeDescriptor.FromTypeSymbol(compilation.GetTypeByFullName(Consts.IResolverTypeFullName)!, compilation),
            null,
            null,
            false,
            false);

        //IServiceProvider
        yield return new ServiceDescriptor(
            Consts.TransientValue,
            TypeDescriptor.FromTypeSymbol(compilation.GetTypeByFullName(Consts.IServiceProviderTypeFullName)!, compilation),
            null,
            null,
            false,
            false);
    }
    public static IEnumerable<ServiceDescriptor> GetAllServices(this Compilation compilation, IEnumerable<ServiceDescriptor>? msReg = null)
        => compilation.GetAllServicesWithSymbols(out _, msReg).Select(item => item.service);
    public static IEnumerable<(ServiceDescriptor service, INamedTypeSymbol? symbol)> GetAllServicesWithSymbols(this Compilation compilation, out IEnumerable<INamedTypeSymbol> symbols, IEnumerable<ServiceDescriptor>? msReg = null)
    {
        //Get attribute registrations
        IEnumerable<(ServiceDescriptor service, INamedTypeSymbol? symbol)> attributeReg = AttributeGeneratorHelper.AllIncludedTypes(compilation).Select(attrReg => (attrReg, default(INamedTypeSymbol?)));

        //Get explicit registrations
#pragma warning disable RS1024 // Symbols should be compared for equality
        var allSymbols = new HashSet<INamedTypeSymbol>(NamedTypeSmbolEqualityComparer.Default);
#pragma warning restore RS1024 // Symbols should be compared for equality
        INamedTypeSymbol? registrarTypeSymbol = Util.GetRegistrarTypeSymbol(compilation);
        HashSet<IMethodSymbol>? registrarNonFactoryMethods = Util.GetRegistrarNonFactoryMethods(registrarTypeSymbol);
        IEnumerable<(ServiceDescriptor service, INamedTypeSymbol? symbol)> explicitReg
            = registrarTypeSymbol is null || registrarNonFactoryMethods is null
            ? Enumerable.Empty<(ServiceDescriptor, INamedTypeSymbol?)>()
            : compilation
            .SyntaxTrees
            .SelectMany(tree =>
            {
                //it is advised against using GetSemanticModel within a diagnostic analyzer to prevent using it too often
                // (e.g after completing a syntax node analysis) because that would result in it being too heavy.
                // however, here we're using it only at the compilation time, times the syntax trees available, which should be okay.
                SemanticModel semanticModel = compilation.GetSemanticModel(tree);
                return
                    tree
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Select(invExp => SyntaxContextReceiver.GetRegisteredType(invExp, semanticModel))
                    .Where(item => item.declarations?.Any() == true)
                    .SelectMany(item =>
                    {
                        allSymbols.Add(item.symbol);
                        return ExplicitGeneratorHelper.TypesToRegister(
                                compilation,
                                ExplicitGeneratorHelper.GetDeclarationOverriddenRegisterMethod(item.declarations, registrarTypeSymbol),
                                registrarNonFactoryMethods)
                        .Select(descriptor => (descriptor, (INamedTypeSymbol?)item.symbol));
                    });
            });
        symbols = allSymbols;

        IEnumerable<(ServiceDescriptor service, INamedTypeSymbol? symbol)> msRegs
            = msReg?.Any() == true
            ? msReg.Select(item => (item, default(INamedTypeSymbol?)))
            : Enumerable.Empty<(ServiceDescriptor, INamedTypeSymbol?)>();
        //Get filtered final registrations
        return
            attributeReg
                .WhereIf(explicitReg.Any(), attrReg => !explicitReg.Any(explReg => attrReg.service.ServiceType == explReg.service.ServiceType))
            .Concat(explicitReg)
            .If(msRegs.Any(), services => services.Concat(
                msRegs!
                .WhereIf(explicitReg.Any(), msReg => !explicitReg.Any(explReg => msReg.service.ServiceType == explReg.service.ServiceType))
                .WhereIf(attributeReg.Any(), msReg => !attributeReg.Any(attrReg => msReg.service.ServiceType == attrReg.service.ServiceType))))
            .Concat(
                GetSpecialServices(compilation)
                .Select(service => (service, default(INamedTypeSymbol?))));
    }
    public static INamedTypeSymbol GetFirstRegistration(this Compilation compilation)
    {
        INamedTypeSymbol? registrarTypeSymbol = Util.GetRegistrarTypeSymbol(compilation);
        HashSet<IMethodSymbol>? registrarNonFactoryMethods = Util.GetRegistrarNonFactoryMethods(registrarTypeSymbol);
        return compilation
            .SyntaxTrees
            .SelectMany(tree =>
            {
                //it is advised against using GetSemanticModel within a diagnostic analyzer to prevent using it too often
                // (e.g after completing a syntax node analysis) because that would result in it being too heavy.
                // however, here we're using it only at the compilation time, times the syntax trees available, which should be okay.
                SemanticModel semanticModel = compilation.GetSemanticModel(tree);
                return
                    tree
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Select(invExp => SyntaxContextReceiver.GetRegisteredType(invExp, semanticModel))
                    .Where(item => item.declarations?.Any() == true)
                    .Select(item => item.symbol);
            })
            .First();
    }
}
