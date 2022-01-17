using ThunderboltIoc;

using ThunderForms.Services;

namespace ThunderDroid.Services
{
    public partial class AndroidServiceRegistration : ThunderboltRegistration
    {
        protected override void Register(IThunderboltRegistrar reg)
        {
            reg.AddSingleton<IPlatformService, AndroidPlatformService>();
        }
    }
}