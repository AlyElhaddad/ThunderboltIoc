using System.Windows;

using ThunderboltIoc;

using ThunderboltIocSamples.WpfCore.Services;

namespace ThunderboltIocSamples.WpfCore
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
