using ThunderboltIocSamples.WpfCore.Services;

namespace ThunderboltIocSamples.WpfCore.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel(DataService dataService, IConventionalService conventionalService)
        {
            DataService = dataService;
            ConventionalService = conventionalService;
        }

        public DataService DataService { get; }
        public IConventionalService ConventionalService { get; }

    }
}
