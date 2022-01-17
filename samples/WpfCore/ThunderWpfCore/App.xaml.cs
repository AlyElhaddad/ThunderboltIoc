using System.Windows;

using ThunderboltIoc;

using ThunderWpfCore.Services;

namespace ThunderWpfCore
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            ThunderboltActivator.Attach<ServiceRegistration>();
        }
    }
}
