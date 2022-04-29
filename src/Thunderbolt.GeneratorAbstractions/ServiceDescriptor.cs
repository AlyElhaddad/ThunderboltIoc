using Microsoft.CodeAnalysis;

namespace Thunderbolt.GeneratorAbstractions;

internal struct ServiceDescriptor : IEquatable<ServiceDescriptor>
{
    internal ServiceDescriptor(
        int? lifetime,
        TypeDescriptor serviceType,
        TypeDescriptor? implType,
        IEnumerable<TypeDescriptor>? implSelectorTypes,
        bool hasFactory,
        bool shouldUseFullDictate)
    {
        Lifetime = lifetime;
        ServiceType = serviceType;
        ImplType = implType;
        ImplSelectorTypes = implSelectorTypes;
        HasFactory = hasFactory;
        ShouldUseFullDictate = shouldUseFullDictate;
    }

    public int? Lifetime { get; set; }
    public TypeDescriptor ServiceType { get; set; }
    public TypeDescriptor? ImplType { get; set; }
    public IEnumerable<TypeDescriptor>? ImplSelectorTypes { get; set; }
    public bool HasFactory { get; set; }
    public bool ShouldUseFullDictate { get; set; }

    public IEnumerable<TypeDescriptor> GetPossibleImplementations(IEnumerable<ServiceDescriptor> allServices)
    {
        if (ImplType is TypeDescriptor implType)
        {
            if (implType.IsImplemented)
            { yield return implType; }
            else
            {
                if (allServices.TryFind(service => service.ServiceType == implType, out ServiceDescriptor serviceDescriptor))
                {
                    foreach (var implImpl in serviceDescriptor.GetPossibleImplementations(allServices))
                        yield return implImpl;
                }
            }
            yield break;
        }

        if (ImplSelectorTypes is IEnumerable<TypeDescriptor> implSelectorTypes)
        {
            foreach (var impl in implSelectorTypes)
            {
                if (allServices.TryFind(service => service.ServiceType == impl, out var serviceDescriptor))
                {
                    foreach (var implImpl in serviceDescriptor.GetPossibleImplementations(allServices))
                        yield return implImpl;
                }
                else if (impl.IsImplemented)
                {
                    yield return impl;
                }
            }
            yield break;
        }

        if (ServiceType.IsImplemented)
            yield return ServiceType;
    }

    public bool IsSpecialService(Compilation compilation)
    {
        return Util.GetSpecialServices(compilation).Contains(this);
    }

    #region Equality
    public override bool Equals(object obj)
    {
        return obj is ServiceDescriptor other && Equals(other);
    }
    public bool Equals(ServiceDescriptor other)
    {
        return ServiceType == other.ServiceType;
    }
    public override int GetHashCode()
    {
        return ServiceType.GetHashCode();
    }
    public static bool operator ==(ServiceDescriptor left, ServiceDescriptor right) => left.Equals(right);
    public static bool operator !=(ServiceDescriptor left, ServiceDescriptor right) => !left.Equals(right);
    #endregion

    public override string ToString()
    {
        return ServiceType.Name;
    }
}
