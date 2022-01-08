using ThunderboltIoc;

using ThunderboltIocSamples.XamarinForms.Services;
using ThunderboltIocSamples.XamarinForms.Views;

using Xamarin.Forms;

namespace ThunderboltIocSamples.XamarinForms
{
    public partial class App : Application
    {
        static App()
        {
            ThunderboltActivator.Attach<SharedServiceRegistration>();
        }

        public App()
        {
            InitializeComponent();

            MainPage = new MainView();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
