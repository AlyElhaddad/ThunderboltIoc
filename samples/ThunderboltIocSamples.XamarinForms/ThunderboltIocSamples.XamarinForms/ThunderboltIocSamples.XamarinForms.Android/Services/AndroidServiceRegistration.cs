using ThunderboltIoc;

using ThunderboltIocSamples.XamarinForms.Services;

namespace ThunderboltIocSamples.XamarinForms.Droid.Services
{
    public partial class AndroidServiceRegistration : ThunderboltRegistration
    {
        protected override void Register(IThunderboltRegistrar reg)
        {
            reg.AddSingleton<IPlatformService, AndroidPlatformService>();
        }
    }
}