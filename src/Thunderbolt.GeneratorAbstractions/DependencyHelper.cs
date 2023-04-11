using Microsoft.CodeAnalysis;

using System.Collections.ObjectModel;
using System.Reflection;

namespace Thunderbolt.GeneratorAbstractions;

internal static class DependencyHelper
{
    internal static IEnumerable<IEnumerable<TypeDescriptor>> FindBestCtors(TypeDescriptor typeDescriptor, IEnumerable<ServiceDescriptor> services)
    {
        var bestCtorsSorted
            = typeDescriptor
            .CtorsParamsTypes
            .Where(ctorTypes =>
                ctorTypes.All(p =>
                    services.Any(s => p.MatchesService(s))))
            .OrderByDescending(ctorTypes => ctorTypes.Count());
        if (!typeDescriptor.IsImplemented || !bestCtorsSorted.Any())
        {
            throw new MissingMethodException($"Could not find a suitable constructor for type '{typeDescriptor.Name.RemovePrefix(Consts.global)}'.");
        }
        return bestCtorsSorted;
    }
    internal static IEnumerable<(string propName, ServiceDescriptor service)> GetInjectedProperties(TypeDescriptor typeDescriptor, IEnumerable<ServiceDescriptor> services)
    {
        return
            typeDescriptor
            .PublicSetProperties
            .SelectWhere((KeyValuePair<string, TypeDescriptor> prop, out (string propName, ServiceDescriptor service) result) =>
            {
                if (services.TryFind(service => service.ServiceType == prop.Value, out var service))
                {
                    result = (prop.Key, service);
                    return true;
                }
                result = default;
                return false;
            });
    }
    private static IEnumerable<ServiceDescriptor> GetDependencies(ServiceDescriptor serviceDescriptor, IEnumerable<ServiceDescriptor> services)
    {
        return
            serviceDescriptor
            .GetPossibleImplementations(services)
            .SelectMany(typeDescriptor =>
                FindBestCtors(typeDescriptor, services)
                .First()
                .SelectMany(type => services.Where(service => type.MatchesService(service)))
                .Concat(
                    GetInjectedProperties(typeDescriptor, services)
                    .Select(prop => prop.service)));
    }

    internal static bool HasCyclicDependencies(
        IEnumerable<ServiceDescriptor> services,
        out IEnumerable<string> cyclicDependencies)
    {
        HashSet<string> cyclicDeps = new();
        bool hasCyclicDependencies
            = HasCyclicDependencies(
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
        foreach (ServiceDescriptor serviceDescriptor in services.Where(service => !service.HasFactory))
        {
            if (visitingServices.Add(serviceDescriptor))
            {
                if (HasCyclicDependencies(GetDependencies(serviceDescriptor, allServices), allServices, visitingServices, visitedServices, cyclicDependencies)
                    && !hasCyclicDependencies)
                {
                    cyclicDependencies.Add(serviceDescriptor.ServiceType.Name);
                    hasCyclicDependencies = true;
                }
                visitedServices.Add(serviceDescriptor);
            }
            else if (!visitedServices.Contains(serviceDescriptor))
            {
                cyclicDependencies.Add(serviceDescriptor.ServiceType.Name);
                hasCyclicDependencies = true;
            }
        }
        return hasCyclicDependencies;
    }
}
