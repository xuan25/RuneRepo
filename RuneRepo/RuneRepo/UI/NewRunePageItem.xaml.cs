using System.Windows.Controls;
using System.Windows.Input;

namespace RuneRepo.UI
{
    /// <summary>
    /// Interaction logic for NewRunePageItem.xaml
    /// </summary>
    public partial class NewRunePageItem : UserControl
    {
        public delegate void StoreNewHendler();
        public event StoreNewHendler StoreNew;

        public NewRunePageItem()
        {
            InitializeComponent();
            InfoBorder.Opacity = 0;
        }

        private void StoreBtn_Clicked(object sender, MouseButtonEventArgs e)
        {
            StoreNew?.Invoke();
        }
    }
}
