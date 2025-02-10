using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Hidden_Hills.MVVM.ViewModel
{
    internal class MainViewModel : ObservableObject
    {
        // Komendy
        public RelayCommand HomeViewCommand { get; set; }
        public RelayCommand PackageViewCommand { get; set; }

        // Obiekty ekranów 
        public PackageViewModel PackageVM { get; set; }
        public HomeViewModel HomeVM { get; set; }

        private object _currentView;

        public object CurrentView
        {
            get { return _currentView; }
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        // Konstruktor tworzący widok startowy 
        // Implementacja komend do przechodzenia pomiędzy ekranami
        public MainViewModel()
        {
            // Tworzenie instancji ViewModeli
            HomeVM = new HomeViewModel();
            PackageVM = new PackageViewModel();
            CurrentView = HomeVM;

            // Inicjalizacja komend
            HomeViewCommand = new RelayCommand(() =>
            {
                CurrentView = HomeVM;
            });

            PackageViewCommand = new RelayCommand(() =>
            {
                CurrentView = PackageVM;
            });
        }
    }
}