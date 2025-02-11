using System.Windows.Controls;

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
            this.DataContext = DecryptViewModel();
        }
    }
}
