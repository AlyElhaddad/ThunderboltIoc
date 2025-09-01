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
    public static bool before = true;

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
            if (!before)
            {
                webAppBuilder.Build();
                //try { webAppBuilder.Build(); } catch { }
                ThunderboltMsRegistration.BuilderServices = webAppBuilder.Services;
                throw new ThunderboltCodeGenerationIntentionalException();
            }
            else
            {
                return webAppBuilder;
            }
        }

        if (before)
        {
            var factory = new ThunderboltServiceProviderFactory<TRegistration>();
            var replacement = ServiceDescriptor.Singleton<IServiceProviderFactory<IServiceCollection>>(factory);
            webAppBuilder.Services.Replace(replacement);
#pragma warning disable ASP0012 // Suggest using builder.Services over Host.ConfigureServices or WebHost.ConfigureServices
            webAppBuilder.WebHost.ConfigureServices(services => services.Replace(replacement));
#pragma warning restore ASP0012 // Suggest using builder.Services over Host.ConfigureServices or WebHost.ConfigureServices
            webAppBuilder.Host.UseServiceProviderFactory(factory);
        }
        return webAppBuilder;
    }
#endif
}
