using Microsoft.Extensions.DependencyInjection;

using ThunderboltIoc;

namespace Thunderbolt.Extensions.Abstractions;

public abstract class ThunderboltMsRegistration : ThunderboltRegistration
{
    internal static bool isGeneratingCode;
    internal static IServiceCollection? BuilderServices;
    public abstract Action<string[]> BuilderAction { get; }
}
