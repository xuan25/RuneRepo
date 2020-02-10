using JsonUtil;
using RuneRepo.ClientUx;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RuneRepo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RequestWrapper RequestWrapper = null;
        
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            NewRunePageItem newRunePageItem = new NewRunePageItem();
            newRunePageItem.StoreNew += NewRunePageItem_StoreNew;
            RunePagePanel.Children.Add(newRunePageItem);
            LoadConfig();
        }

        private void NewRunePageItem_StoreNew()
        {
            if (ValidateRequestWrapper())
            {
                Json.Value value = RequestWrapper.GetCurrentRunePage();
                AppendRunePage(value);
                UpdateConfig();
            }
            else
            {
                MessageBox.Show("Not avaliable", "Not avaliable");
            }
        }

        private void LoadConfig()
        {
            if (!File.Exists("Config.json"))
                return;
            using (StreamReader streamReader = new StreamReader("Config.json"))
            {
                string config = streamReader.ReadToEnd();
                Json.Value runePageArray = Json.Parser.Parse(config);
                foreach (Json.Value value in runePageArray)
                {
                    AppendRunePage(value);
                }
            }
        }

        private void AppendRunePage(Json.Value value)
        {
            RunePageItem runePageItem = new RunePageItem(value);
            runePageItem.Apply += RunePageItem_Apply;
            runePageItem.Delete += RunePageItem_Delete;
            runePageItem.Updated += RunePageItem_Updated;
            RunePagePanel.Children.Insert(RunePagePanel.Children.Count - 1, runePageItem);
        }

        private void RunePageItem_Updated(RunePageItem runePageItem)
        {
            UpdateConfig();
        }

        private void RunePageItem_Delete(RunePageItem runePageItem)
        {
            if (MessageBox.Show(string.Format("Delete runepage \"{0}\" ?", runePageItem.PageName), "Delete confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                RunePagePanel.Children.Remove(runePageItem);
            }
            UpdateConfig();
        }

        private void RunePageItem_Apply(Json.Value value)
        {
            if (ValidateRequestWrapper())
            {
                Json.Value currentPageJson = RequestWrapper.GetCurrentRunePage();
                if(currentPageJson != null)
                {
                    ulong cid = currentPageJson["id"];
                    if(currentPageJson["isDeletable"])
                        RequestWrapper.DeleteRunePage(cid);
                }
                RequestWrapper.AddRunePage(value);
            }
            else
            {
                MessageBox.Show("Not avaliable", "Not avaliable");
            }
        }

        private void UpdateConfig()
        {
            Json.Value.Array runePageArray = new Json.Value.Array();

            for(int i = 0; i < RunePagePanel.Children.Count - 1; i++)
            {
                runePageArray.Add(((RunePageItem)RunePagePanel.Children[i]).Value);
            }

            using (StreamWriter streamWriter = new StreamWriter("Config.json"))
            {
                streamWriter.Write(runePageArray.ToString());
            }
        }

        private bool ValidateRequestWrapper()
        {
            if(RequestWrapper != null)
            {
                if (RequestWrapper.IsAvaliable)
                    return true;
            }
            string lolPath = ClientLocator.GetLolPath();
            if (lolPath == null)
                return false;
            RequestWrapper = new RequestWrapper(lolPath);
            return RequestWrapper.IsAvaliable;
        }

        // TODO : Remove
        private void RunePageNameBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                TextBox textBox = (TextBox)((Grid)((TextBlock)sender).Parent).Children[1];
                textBox.Visibility = Visibility.Visible;
                Dispatcher.Invoke(async () =>
                {
                    await Task.Delay(1);
                    textBox.SelectAll();
                    textBox.Focus();
                    textBox.LostFocus += RunePageNameEditBox_LostFocus;
                });
            }
        }

        // TODO : Remove
        private void RunePageNameEditBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            textBox.LostFocus -= RunePageNameEditBox_LostFocus;
            textBox.Visibility = Visibility.Hidden;
            UpdateConfig();
        }

        // TODO : Remove
        private void RunePageNameEditBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                ((TextBox)sender).Visibility = Visibility.Hidden;
                UpdateConfig();
            }
        }
    }
}
