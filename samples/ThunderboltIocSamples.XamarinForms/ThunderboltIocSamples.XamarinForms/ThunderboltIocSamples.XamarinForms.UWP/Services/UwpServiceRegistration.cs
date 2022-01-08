using ThunderboltIoc;

using ThunderboltIocSamples.XamarinForms.Services;

namespace ThunderboltIocSamples.XamarinForms.UWP.Services
{
    public partial class UwpServiceRegistration : ThunderboltRegistration
    {
        protected override void Register(IThunderboltRegistrar reg)
        {
            reg.AddSingleton<IPlatformService, UwpPlatformService>();
        }
    }
}
