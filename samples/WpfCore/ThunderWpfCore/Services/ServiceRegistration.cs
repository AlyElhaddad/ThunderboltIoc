using ThunderboltIoc;

//Match IConventionalService with ConventionalService
[assembly: ThunderboltRegexInclude(
    ThunderboltServiceLifetime.Scoped,
    regex: @"(global::)?(ThunderWpfCore\.Services)\.I[A-Z][A-z_]+Service",
    implRegex: @"(global::)?(ThunderWpfCore\.Services)\.[A-Z][a-z][A-z_]+Service",
    joinKeyRegex: @"(?<=(global::)?(ThunderWpfCore\.Services)\.I?)[A-Z][a-z][A-z_]+Service")]

//Include all ViewModels
[assembly: ThunderboltRegexInclude(
    ThunderboltServiceLifetime.Transient,
    regex: @"(global::)?(ThunderWpfCore\.ViewModels)\.[A-Z][A-z_]+ViewModel")]

namespace ThunderWpfCore.Services
{
    public partial class ServiceRegistration : ThunderboltRegistration
    {
        protected override void Register(IThunderboltRegistrar reg)
        {
            reg.AddSingleton<DataService>();
        }
    }
}
