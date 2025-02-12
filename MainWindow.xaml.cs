using System.Windows;
using Hidden_Hills.MVVM.ViewModel;

namespace Hidden_Hills
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel();
        }

        private void CloseApplication() => Application.Current.Shutdown();

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseApplication();
        }


    }
}
