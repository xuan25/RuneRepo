using Common;
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
using System.Windows.Shapes;

namespace RuneRepo
{
    /// <summary>
    /// Interaction logic for IconWindow.xaml
    /// </summary>
    public partial class WidgetWindow : Window
    {
        Window MainWindow { get; set; }
        private AttachWindowCore AttachCore;

        public WidgetWindow(Window mainWindow)
        {
            InitializeComponent();
            MainWindow = mainWindow;

            this.Closing += WidgitWindow_Closing;
        }

        private void WidgitWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MainWindow = null;
        }

        private void RestoreBtn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.WindowState = WindowState.Normal;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            AttachCore = new AttachWindowCore();
            AttachCore.Init(this);
            base.OnSourceInitialized(e);
        }

        public void AttachToClient()
        {
            Rect clientRect = AttachCore.GetClientWindowRect();
            this.Left = clientRect.Width - 150;
            this.Top = 7;

            AttachCore.AttachToClient();
        }

        public void Detach()
        {
            AttachCore.Detach();
        }
    }
}
