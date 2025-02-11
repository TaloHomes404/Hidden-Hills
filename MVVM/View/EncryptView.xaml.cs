using System.Windows.Controls;
using Hidden_Hills.MVVM.ViewModel;

namespace Hidden_Hills.MVVM.View
{
    /// <summary>
    /// Logika interakcji dla klasy EncryptView.xaml
    /// </summary>
    public partial class EncryptView : UserControl
    {
        public EncryptView()
        {
            InitializeComponent();
            this.DataContext = new EncryptViewModel();
        }
    }
}
