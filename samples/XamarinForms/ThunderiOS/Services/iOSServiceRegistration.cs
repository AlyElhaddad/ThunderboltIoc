using ThunderboltIoc;

using ThunderForms.Services;

namespace ThunderiOS.Services
{
    public partial class iOSServiceRegistration : ThunderboltRegistration
    {
        protected override void Register(IThunderboltRegistrar reg)
        {
            reg.AddSingleton<IPlatformService, iOSPlatformService>();
        }
    }
}