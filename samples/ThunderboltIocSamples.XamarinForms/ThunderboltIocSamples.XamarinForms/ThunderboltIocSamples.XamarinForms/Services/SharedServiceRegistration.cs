using ThunderboltIoc;

//Match IConventionalService with ConventionalService
[assembly: ThunderboltRegexInclude(
    ThunderboltServiceLifetime.Scoped,
    regex: @"(global::)?(ThunderboltIocSamples\.XamarinForms\.Services)\.I[A-Z][A-z_]+Service",
    implRegex: @"(global::)?(ThunderboltIocSamples\.XamarinForms\.Services)\.[A-Z][a-z][A-z_]+Service",
    joinKeyRegex: @"(?<=(global::)?(ThunderboltIocSamples\.XamarinForms\.Services)\.I?)[A-Z][a-z][A-z_]+Service")]

//Include all ViewModels
[assembly: ThunderboltRegexInclude(
    ThunderboltServiceLifetime.Transient,
    regex: @"(global::)?(ThunderboltIocSamples\.XamarinForms\.ViewModels)\.[A-Z][A-z_]+ViewModel")]

namespace ThunderboltIocSamples.XamarinForms.Services
{
    public partial class SharedServiceRegistration : ThunderboltRegistration
    {
        protected override void Register(IThunderboltRegistrar reg)
        {
            reg.AddSingleton<DataService>();
        }
    }
}
