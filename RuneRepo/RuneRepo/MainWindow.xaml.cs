using JsonUtil;
using RuneRepo.ClientUx;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

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

        private async void NewRunePageItem_StoreNew()
        {
            if (await ValidateRequestWrapperAsync())
            {
                Json.Value value = await RequestWrapper.GetCurrentRunePageAsync();

                if(value != null)
                {
                    value.Remove("current");
                    value.Remove("id");
                    value.Remove("isActive");
                    value.Remove("isDeletable");
                    value.Remove("isEditable");
                    value.Remove("isValid");
                    value.Remove("lastModified");
                    value.Remove("order");

                    AppendRunePage(value);
                    UpdateConfig();
                }
                else {
                    MessagePopup messagePopup = new MessagePopup("Please select a page, try again?");
                    messagePopup.Decided += delegate (bool result)
                    {
                        if (result)
                        {
                            NewRunePageItem_StoreNew();
                        }
                        MainViewGrid.Children.Remove(messagePopup);
                    };
                }
            }
            else
            {
                MessagePopup messagePopup = new MessagePopup("Service currently unavailable, try again?");
                messagePopup.Decided += delegate (bool result)
                {
                    if (result)
                    {
                        NewRunePageItem_StoreNew();
                    }
                    MainViewGrid.Children.Remove(messagePopup);
                };
                MainViewGrid.Children.Add(messagePopup);
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

            Border border = new Border() { Height = 277, Width = 176, Margin = new Thickness(14), Background = new SolidColorBrush(Colors.Black)};
            RunePagePanelDummy.Children.Add(border);
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

            HitTestResult hitTestResult = VisualTreeHelper.HitTest(RunePagePanelDummy, e.GetPosition(RunePagePanelDummy));
            if (hitTestResult != null)
            {
                Border dragOverPageItemDummy = FindVisualParent<Border>(hitTestResult.VisualHit);
                int overIndex = RunePagePanelDummy.Children.IndexOf(dragOverPageItemDummy);
                RunePageItem dragOverPageItem = (RunePageItem)RunePagePanel.Children[overIndex];
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
                    RunePagePanelDummy.Children.RemoveAt(RunePagePanelDummy.Children.Count - 1);
                    UpdateConfig();
                }
                MainViewGrid.Children.Remove(messagePopup);
            };
            MainViewGrid.Children.Add(messagePopup);
        }

        private async void RunePageItem_Apply(Json.Value value)
        {
            if (await ValidateRequestWrapperAsync())
            {
                Json.Value currentPageJson = await RequestWrapper.GetCurrentRunePageAsync();
                int order = 0;
                if(currentPageJson != null)
                {
                    ulong selectedId = currentPageJson["id"];
                    if(currentPageJson["isDeletable"])
                        await RequestWrapper.DeleteRunePageAsync(selectedId);
                }
                value["order"] = order;
                if(!await RequestWrapper.AddRunePageAsync(value))
                {
                    Json.Value pages = await RequestWrapper.GetRunePages();
                    foreach(Json.Value pageJson in pages)
                    {
                        if (pageJson["isDeletable"])
                        {
                            value["order"] = pageJson["order"];
                            await RequestWrapper.DeleteRunePageAsync(pageJson["id"]);
                            await RequestWrapper.AddRunePageAsync(value);
                            break;
                        }
                    }
                }
            }
            else
            {
                MessagePopup messagePopup = new MessagePopup("Service currently unavailable, try again?");
                messagePopup.Decided += delegate (bool result)
                {
                    if (result)
                    {
                        RunePageItem_Apply(value);
                    }
                    MainViewGrid.Children.Remove(messagePopup);
                };
                MainViewGrid.Children.Add(messagePopup);
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

        private async Task<bool> ValidateRequestWrapperAsync()
        {
            if(RequestWrapper != null)
            {
                if (await RequestWrapper.CheckAvaliableAsync())
                    return true;
            }
            string lolPath = ClientLocator.GetLolPath();
            if (lolPath == null)
                return false;
            RequestWrapper = new RequestWrapper(lolPath);
            return await RequestWrapper.CheckAvaliableAsync();
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

        private void RunePageDummy_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if(e.HorizontalChange != 0 || e.VerticalChange != 0)
            {
                Storyboard storyboard = new Storyboard();

                if(e.HorizontalChange != 0)
                {
                    ScrollBar horizontalDummy = (ScrollBar)RunePageDummy.Template.FindName("PART_HorizontalScrollBarDummy", RunePageDummy);

                    DoubleAnimation horizontalAnimation = new DoubleAnimation() { To = e.VerticalOffset, Duration = TimeSpan.FromSeconds(0.2), EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut } };
                    Storyboard.SetTarget(horizontalAnimation, horizontalDummy);
                    Storyboard.SetTargetProperty(horizontalAnimation, new PropertyPath(ScrollBar.ValueProperty));
                    storyboard.Children.Add(horizontalAnimation);
                }

                if (e.VerticalChange != 0)
                {
                    ScrollBar verticalDummy = (ScrollBar)RunePageDummy.Template.FindName("PART_VerticalScrollBarDummy", RunePageDummy);

                    DoubleAnimation verticalAnimation = new DoubleAnimation() { To = e.VerticalOffset, Duration = TimeSpan.FromSeconds(0.2), EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut } };
                    Storyboard.SetTarget(verticalAnimation, verticalDummy);
                    Storyboard.SetTargetProperty(verticalAnimation, new PropertyPath(ScrollBar.ValueProperty));
                    storyboard.Children.Add(verticalAnimation);
                }

                storyboard.Begin();
            }
        }
    }
}
