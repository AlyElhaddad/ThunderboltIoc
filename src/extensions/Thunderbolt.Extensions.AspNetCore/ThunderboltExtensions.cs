#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
#endif
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Thunderbolt.Extensions.Abstractions;

namespace Thunderbolt.Extensions;

public static class ThunderboltExtensions
{
    public static IWebHostBuilder UseThunderbolt<TRegistration>(this IWebHostBuilder hostBuilder)
        where TRegistration : notnull, ThunderboltMsRegistration, new()
    {
        return hostBuilder.ConfigureServices(services =>
        {
            if (ThunderboltMsRegistration.isGeneratingCode)
                ThunderboltMsRegistration.BuilderServices = services;
            else
                services.Replace(ServiceDescriptor.Singleton<IServiceProviderFactory<IServiceCollection>>(new ThunderboltServiceProviderFactory<TRegistration>()));
        });
    }
#if NET6_0_OR_GREATER
    public static WebApplicationBuilder UseThunderbolt<TRegistration>(this WebApplicationBuilder webAppBuilder)
        where TRegistration : notnull, ThunderboltMsRegistration, new()
    {
        if (ThunderboltMsRegistration.isGeneratingCode)
        {
            ThunderboltMsRegistration.BuilderServices = webAppBuilder.Services;
            return webAppBuilder;
        }
        var replacement = ServiceDescriptor.Singleton<IServiceProviderFactory<IServiceCollection>>(new ThunderboltServiceProviderFactory<TRegistration>());
        webAppBuilder.Services.Replace(replacement);
        webAppBuilder.WebHost.ConfigureServices(services => services.Replace(replacement));
        webAppBuilder.Host.UseServiceProviderFactory(new ThunderboltServiceProviderFactory<TRegistration>());
        return webAppBuilder;
    }
#endif
}
