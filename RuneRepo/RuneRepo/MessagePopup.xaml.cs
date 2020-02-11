using System.Windows.Controls;
using System.Windows.Input;

namespace RuneRepo
{
    /// <summary>
    /// Interaction logic for MessagePopup.xaml
    /// </summary>
    public partial class MessagePopup : UserControl
    {
        public delegate void DecidedHandler(bool result);
        public event DecidedHandler Decided;

        public MessagePopup()
        {
            InitializeComponent();
        }

        public MessagePopup(string message)
        {
            InitializeComponent();
            MessageBox.Text = message;
        }

        private void YesBtn_Clicked(object sender, MouseButtonEventArgs e)
        {
            Decided?.Invoke(true);
        }

        private void NoBtn_Clicked(object sender, MouseButtonEventArgs e)
        {
            Decided?.Invoke(false);
        }
    }
}
