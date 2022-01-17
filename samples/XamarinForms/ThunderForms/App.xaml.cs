using ThunderboltIoc;

using ThunderForms.Services;
using ThunderForms.Views;

using Xamarin.Forms;

namespace ThunderForms
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
