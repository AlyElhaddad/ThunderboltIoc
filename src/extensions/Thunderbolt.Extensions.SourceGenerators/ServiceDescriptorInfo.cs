using System.Reflection;

namespace Thunderbolt.Extensions.SourceGenerators;

internal readonly struct ServiceDescriptorInfo
{
    public ServiceDescriptorInfo(int lifetime, string fullName, bool isExternalNonPublicType, string? implFullName, bool? implIsExternalNonPublicType, bool hasFactory)
    {
        Lifetime = lifetime;
        FullName = fullName;
        IsExternalNonPublicType = isExternalNonPublicType;
        ImplFullName = implFullName;
        ImplIsExternalNonPublicType = implIsExternalNonPublicType;
        HasFactory = hasFactory;
    }
    
    public int Lifetime { get; }
    public string FullName { get; }
    public bool IsExternalNonPublicType { get; }
    public string? ImplFullName { get; }
    public bool? ImplIsExternalNonPublicType { get; }
    public bool HasFactory { get; }
}
