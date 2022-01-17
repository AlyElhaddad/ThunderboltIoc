using ThunderboltIoc;

//Match IConventionalService with ConventionalService
[assembly: ThunderboltRegexInclude(
    ThunderboltServiceLifetime.Scoped,
    regex: @"(global::)?(ThunderForms\.Services)\.I[A-Z][A-z_]+Service",
    implRegex: @"(global::)?(ThunderForms\.Services)\.[A-Z][a-z][A-z_]+Service",
    joinKeyRegex: @"(?<=(global::)?(ThunderForms\.Services)\.I?)[A-Z][a-z][A-z_]+Service")]

//Include all ViewModels
[assembly: ThunderboltRegexInclude(
    ThunderboltServiceLifetime.Transient,
    regex: @"(global::)?(ThunderForms\.ViewModels)\.[A-Z][A-z_]+ViewModel")]

namespace ThunderForms.Services
{
    public partial class SharedServiceRegistration : ThunderboltRegistration
    {
        protected override void Register(IThunderboltRegistrar reg)
        {
            reg.AddSingleton<DataService>();
        }
    }
}
