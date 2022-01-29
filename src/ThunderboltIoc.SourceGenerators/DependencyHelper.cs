using Microsoft.CodeAnalysis;

namespace ThunderboltIoc.SourceGenerators;

internal static class DependencyHelper
{
    internal static IEnumerable<IMethodSymbol> FindBestCtors(INamedTypeSymbol symbol, IEnumerable<INamedTypeSymbol> services)
    {
        var bestCtorsSorted
            = symbol
              .InstanceConstructors
              .Where(ctor =>
              {
                  return
                      ctor.DeclaredAccessibility == Accessibility.Public
                      && ctor.Parameters.All(p =>
                          {
                              string typeName = p.Type.GetFullyQualifiedName();
                              return services.Any(s => s.GetFullyQualifiedName() == typeName);
                          });
              }).OrderBy(ctor => ctor.Parameters.Length);
        if (symbol.IsAbstract || !bestCtorsSorted.Any())
        {
            throw new MissingMethodException($"Could not find a suitable constructor for type '{symbol.GetFullyQualifiedName().RemovePrefix(Consts.global)}'.");
        }
        return bestCtorsSorted;
    }
    internal static IEnumerable<IMethodSymbol> FindBestCtors(INamedTypeSymbol symbol, IEnumerable<ServiceDescriptor> services)
    {
        return FindBestCtors(symbol, services.Select(service => service.ServiceSymbol));
    }

    private static IEnumerable<ServiceDescriptor> GetDependencies(ServiceDescriptor serviceDescriptor, IEnumerable<ServiceDescriptor> services)
    {
        return serviceDescriptor.GetPossibleImplementations(services).SelectMany(symbol => FindBestCtors(symbol, services).First().Parameters.Select(p => p.Type).OfType<INamedTypeSymbol>().Select(type => services.First(service => type.GetFullyQualifiedName() == service.ServiceSymbol.GetFullyQualifiedName())));
    }

    internal static bool HasCyclicDependencies(
        IEnumerable<ServiceDescriptor> services,
        out IEnumerable<string> cyclicDependencies)
    {
        HashSet<string> cyclicDeps = new();
        bool hasCyclicDependencies =
            HasCyclicDependencies(
                services,
                services,
                new HashSet<ServiceDescriptor>(),
                new HashSet<ServiceDescriptor>(),
                cyclicDeps);
        cyclicDependencies = cyclicDeps;
        return hasCyclicDependencies;
    }
    private static bool HasCyclicDependencies(
        IEnumerable<ServiceDescriptor> services,
        IEnumerable<ServiceDescriptor> allServices,
        HashSet<ServiceDescriptor> visitingServices,
        HashSet<ServiceDescriptor> visitedServices,
        HashSet<string> cyclicDependencies)
    {
        bool hasCyclicDependencies = false;
        foreach (ServiceDescriptor serviceDescriptor in services)
        {
            if (visitingServices.Add(serviceDescriptor))
            {
                if (HasCyclicDependencies(GetDependencies(serviceDescriptor, allServices), allServices, visitingServices, visitedServices, cyclicDependencies)
                    && !hasCyclicDependencies)
                {
                    cyclicDependencies.Add(serviceDescriptor.ServiceSymbol.GetFullyQualifiedName());
                    hasCyclicDependencies = true;
                }
                visitedServices.Add(serviceDescriptor);
            }
            else if (!visitedServices.Contains(serviceDescriptor))
            {
                cyclicDependencies.Add(serviceDescriptor.ServiceSymbol.GetFullyQualifiedName());
                hasCyclicDependencies = true;
            }
        }
        return hasCyclicDependencies;
    }
}
