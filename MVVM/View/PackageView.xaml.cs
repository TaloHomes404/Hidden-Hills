using System.Windows.Controls;
using Hidden_Hills.MVVM.ViewModel;


namespace Hidden_Hills.MVVM.View

{



    /// <summary>

    /// Logika interakcji dla klasy PackageView.xaml

    /// </summary>

    public partial class PackageView : UserControl

    {

        public PackageView()

        {

            InitializeComponent();
            DataContext = new PackageViewModel();

        }


    }

}
