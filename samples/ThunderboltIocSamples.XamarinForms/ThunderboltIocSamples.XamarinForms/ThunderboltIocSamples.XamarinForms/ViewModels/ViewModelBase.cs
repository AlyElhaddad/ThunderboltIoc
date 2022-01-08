using System.ComponentModel;
using System.Runtime.CompilerServices;

using ThunderboltIoc;

namespace ThunderboltIocSamples.XamarinForms.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        private IThunderboltContainer container;
        protected IThunderboltContainer Container => container ?? (container = ThunderboltActivator.Container);

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
