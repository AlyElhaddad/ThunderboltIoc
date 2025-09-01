namespace Thunderbolt.GeneratorAbstractions;

internal struct GeneratorConfig
{
    public static GeneratorConfig Instance { get; set; }

    public string? StartupArgs { get; set; }
    public bool? EnablePropertyInjection { get; set; }
}
