using ThunderboltIoc;

using ThunderboltIocSamples.XamarinForms.Services;

namespace ThunderboltIocSamples.XamarinForms.iOS.Services
{
    public partial class iOSServiceRegistration : ThunderboltRegistration
    {
        protected override void Register(IThunderboltRegistrar reg)
        {
            reg.AddSingleton<IPlatformService, iOSPlatformService>();
        }
    }
}