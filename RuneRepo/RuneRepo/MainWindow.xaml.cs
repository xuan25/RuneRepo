using JsonUtil;
using RuneRepo.ClientUx;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
            runePageItem.MouseMove += RunePageItem_MouseMove;
            RunePagePanel.Children.Insert(RunePagePanel.Children.Count - 1, runePageItem);
        }

        private bool dragInit = false;
        private Point dragInitPos;
        private void RunePageItem_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                if (dragInit)
                {
                    Point currentPos = e.GetPosition(this);
                    if(Math.Abs(dragInitPos.X - currentPos.X) > 5 || Math.Abs(dragInitPos.Y - currentPos.Y) > 5)
                    {
                        RunePageItem runePageItem = (RunePageItem)sender;

                        DragViewImage.Source = runePageItem.GetDraggingSnapshot(e, new Point(dragInitPos.X - currentPos.X, dragInitPos.Y - currentPos.Y));
                        DragDrop.DoDragDrop(RunePagePanel, runePageItem, DragDropEffects.Move);
                        DragViewImage.Source = null;

                        UpdateConfig();
                        dragInit = false;
                    }
                }
                else
                {
                    dragInit = true;
                    dragInitPos = e.GetPosition(this);
                }
            }
        }

        private void RunePageViewer_DragOver(object sender, DragEventArgs e)
        {
            RunePageItem draggingPageItem = (RunePageItem)e.Data.GetData(typeof(RunePageItem));

            Point overCanvasPoint = e.GetPosition(DragViewCanvas);
            Canvas.SetLeft(DragViewImage, overCanvasPoint.X - draggingPageItem.SnapShotMousePosition.X);
            Canvas.SetTop(DragViewImage, overCanvasPoint.Y - draggingPageItem.SnapShotMousePosition.Y);

            HitTestResult hitTestResult = VisualTreeHelper.HitTest(RunePagePanel, e.GetPosition(RunePagePanel));
            if (hitTestResult != null)
            {
                RunePageItem dragOverPageItem = FindVisualParent<RunePageItem>(hitTestResult.VisualHit);
                if (dragOverPageItem != null && dragOverPageItem != draggingPageItem)
                {
                    int newIndex = RunePagePanel.Children.IndexOf(dragOverPageItem);
                    RunePagePanel.Children.Remove(draggingPageItem);
                    RunePagePanel.Children.Insert(newIndex, draggingPageItem);
                }
            }
        }

        public static T FindVisualParent<T>(DependencyObject obj) where T : class
        {
            while (obj != null)
            {
                if (obj is T)
                    return obj as T;

                obj = VisualTreeHelper.GetParent(obj);
            }
            return null;
        }

        private void RunePageItem_Updated(RunePageItem runePageItem)
        {
            UpdateConfig();
        }

        private void RunePageItem_Delete(RunePageItem runePageItem)
        {
            MessagePopup messagePopup = new MessagePopup("Are you sure you want to delete this page?");
            messagePopup.Decided += delegate (bool result)
            {
                if (result)
                {
                    RunePagePanel.Children.Remove(runePageItem);
                    UpdateConfig();
                }
                MainViewGrid.Children.Remove(messagePopup);
            };
            MainViewGrid.Children.Add(messagePopup);
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

        private void CloseWindowBtn_Clicked(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void MaximizeWindowBtn_Clicked(object sender, MouseButtonEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }

        private void MinimizeWindowBtn_Clicked(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}
