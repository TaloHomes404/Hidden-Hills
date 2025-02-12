using System.Windows.Controls;
using Hidden_Hills.MVVM.ViewModel;
namespace Hidden_Hills.MVVM.View
{
    /// <summary>
    /// Logika interakcji dla klasy DecryptView.xaml
    /// </summary>
    public partial class DecryptView : UserControl
    {
        public DecryptView()
        {
            InitializeComponent();
            this.DataContext = new DecryptViewModel();
        }
    }
}
