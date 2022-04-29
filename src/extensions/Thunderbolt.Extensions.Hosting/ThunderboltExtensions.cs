using Microsoft.Extensions.Hosting;

using Thunderbolt.Extensions.Abstractions;

namespace Thunderbolt.Extensions;

public static class ThunderboltExtensions
{
    public static IHostBuilder UseThunderbolt<TRegistration>(this IHostBuilder hostBuilder)
        where TRegistration : notnull, ThunderboltMsRegistration, new()
    {
        return ThunderboltMsRegistration.isGeneratingCode
             ? hostBuilder.ConfigureServices(services => ThunderboltMsRegistration.BuilderServices = services)
             : hostBuilder.UseServiceProviderFactory(new ThunderboltServiceProviderFactory<TRegistration>());
    }
}
