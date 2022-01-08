using ThunderboltIoc;

//Match IConventionalService with ConventionalService
[assembly: ThunderboltRegexInclude(
    ThunderboltServiceLifetime.Scoped,
    regex: @"(global::)?(ThunderboltIocSamples\.WpfCore\.Services)\.I[A-Z][A-z_]+Service",
    implRegex: @"(global::)?(ThunderboltIocSamples\.WpfCore\.Services)\.[A-Z][a-z][A-z_]+Service",
    joinKeyRegex: @"(?<=(global::)?(ThunderboltIocSamples\.WpfCore\.Services)\.I?)[A-Z][a-z][A-z_]+Service")]

//Include all ViewModels
[assembly: ThunderboltRegexInclude(
    ThunderboltServiceLifetime.Transient,
    regex: @"(global::)?(ThunderboltIocSamples\.WpfCore\.ViewModels)\.[A-Z][A-z_]+ViewModel")]

namespace ThunderboltIocSamples.WpfCore.Services
{
    public partial class ServiceRegistration : ThunderboltRegistration
    {
        protected override void Register(IThunderboltRegistrar reg)
        {
            reg.AddSingleton<DataService>();
        }
    }
}
