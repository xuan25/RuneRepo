using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RuneRepo
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
