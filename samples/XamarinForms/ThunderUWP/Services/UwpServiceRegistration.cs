using ThunderboltIoc;

using ThunderForms.Services;

namespace ThunderUWP.Services
{
    public partial class UwpServiceRegistration : ThunderboltRegistration
    {
        protected override void Register(IThunderboltRegistrar reg)
        {
            reg.AddSingleton<IPlatformService, UwpPlatformService>();
        }
    }
}
