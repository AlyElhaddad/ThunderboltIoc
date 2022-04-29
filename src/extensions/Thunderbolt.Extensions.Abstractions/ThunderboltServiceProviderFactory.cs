﻿using Microsoft.Extensions.DependencyInjection;

using Thunderbolt.GeneratorAbstractions;

using ThunderboltIoc;

namespace Thunderbolt.Extensions.Abstractions;

internal class ThunderboltServiceProviderFactory<TRegistration> : IServiceProviderFactory<IServiceCollection>
    where TRegistration : notnull, ThunderboltMsRegistration, new()
{
    private static readonly System.Reflection.Assembly thisAssembly = typeof(TRegistration).Assembly;

    public IServiceCollection CreateBuilder(IServiceCollection services)
    {
        return services;
    }

    public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
    {
        var privateTypes = (ThunderboltRegistration.PrivateTypes as Dictionary<string, PrivateType>)!;
        foreach (var serviceDescriptor in containerBuilder)
        {
            var serviceType = serviceDescriptor.ServiceType;
            var serviceTypeName = serviceType.GetFullyQualifiedName();
            bool isExternalNonPublicType = serviceType.TendsToExternalNonPublic(thisAssembly);

            var implType = serviceDescriptor.ImplementationType;
            bool implIsExternalNonPublicType = implType is not null && implType.TendsToExternalNonPublic(thisAssembly);
            string? implTypeName = implType?.GetFullyQualifiedName();

            if (isExternalNonPublicType || implIsExternalNonPublicType || serviceType.IsNonClosedGenericType())
            {
                privateTypes.TryAdd(serviceTypeName, new PrivateType(serviceType, null));
            }
            if (implIsExternalNonPublicType || implType?.IsNonClosedGenericType() == true)
            {
                privateTypes.TryAdd(implTypeName!, new PrivateType(implType!, null));
            }

            var additionalTypes
                = serviceType
                .GetGenericArguments()
                .Where(genArg => genArg.IsGenericParameter || genArg.TendsToExternalNonPublic(thisAssembly))
                .Concat(serviceType.GetConstructors().SelectMany(ctor => ctor.GetParameters().Select(ctorParam => ctorParam.ParameterType).Where(ctorParamType => ctorParamType.IsNonClosedGenericType() || ctorParamType.TendsToExternalNonPublic(thisAssembly))));
            var additionalImplTypes
                = implType is null
                ? Enumerable.Empty<Type>()
                : implType
                .GetGenericArguments()
                .Where(genArg => genArg.IsGenericParameter || genArg.TendsToExternalNonPublic(thisAssembly))
                .Concat(implType.GetConstructors().SelectMany(ctor => ctor.GetParameters().Select(ctorParam => ctorParam.ParameterType).Where(ctorParamType => ctorParamType.IsNonClosedGenericType() || ctorParamType.TendsToExternalNonPublic(thisAssembly))));
            var allAdditionalTypes
                = additionalTypes
                .Concat(additionalImplTypes);
            foreach (Type additionalType in allAdditionalTypes)
                privateTypes.TryAdd(additionalType.GetFullyQualifiedName(), new PrivateType(additionalType, null));

            if (serviceDescriptor.ImplementationInstance is not null)
            {
                object instance = serviceDescriptor.ImplementationInstance;
                privateTypes[serviceTypeName] = new PrivateType(serviceType, serviceProvider => instance);
            }
            else if (serviceDescriptor.ImplementationFactory is not null)
            {
                Func<IServiceProvider, object> factory = serviceDescriptor.ImplementationFactory;
                privateTypes[serviceTypeName] = new PrivateType(serviceType, factory);
            }
        }
        ThunderboltActivator.Attach<ThunderboltMsContainer, TRegistration>();
        return ThunderboltActivator.Container;
    }
}