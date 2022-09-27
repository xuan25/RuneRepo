using Common;
using JsonUtil;
using RuneRepo.ClientUx;
using RuneRepo.UI;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace RuneRepo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const float uiScaleFactor = 0.75f;
        private const string RepoFile = "repository.json";
        private const string ConfigFile = "config.json";

        private RequestWrapper Wrapper = null;
        private GameflowPhaseMonitor PhaseMonitor = null;

        private AttachWindowCore AttachCore;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            AttachCore = new AttachWindowCore();
            AttachCore.Init(this);
            AttachCore.TargetLostDetached += AttachCore_TargetLostDetached;

            HotKey.Register(this, HotKey.Modifier.MOD_ALT | HotKey.Modifier.MOD_SHIFT, VirtualKey.KEY_R, out int id, () =>
            {
                this.WindowState = this.WindowState == WindowState.Minimized ? WindowState.Normal : WindowState.Minimized;
            });

            base.OnSourceInitialized(e);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                LoadRepo();

                Wrapper = new RequestWrapper();
                PhaseMonitor = new GameflowPhaseMonitor(Wrapper);
                PhaseMonitor.PhaseChanged += PhaseMonitor_PhaseChanged;
                PhaseMonitor.Start();
            });
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveConfig();

            Application.Current.Shutdown();
        }

        Rect WndPosNormal = new Rect(0, 0, 230, 340);
        Rect WndPosAttached = new Rect(0, 0, 230, 340);

        public void LoadConfig()
        {
            if (!File.Exists(ConfigFile))
                return;
            Json.Value json = null;
            using (FileStream fileStream = new FileStream(ConfigFile, FileMode.Open, FileAccess.Read))
            {
                json = Json.Parser.Parse(fileStream);
            }

            if (json.Contains("width"))
                WndPosNormal.Width = json["width"];
            if (json.Contains("height"))
                WndPosNormal.Height = json["height"];
            if (json.Contains("left"))
                WndPosNormal.X = json["left"];
            if (json.Contains("top"))
                WndPosNormal.Y = json["top"];

            if (json.Contains("width_attached"))
                WndPosAttached.Width = json["width_attached"];
            if (json.Contains("height_attached"))
                WndPosAttached.Height = json["height_attached"];
            if (json.Contains("left_attached"))
                WndPosAttached.X = json["left_attached"];
            if (json.Contains("top_attached"))
                WndPosAttached.Y = json["top_attached"];

            this.Width = WndPosNormal.Width;
            this.Height = WndPosNormal.Height;
            this.Left = WndPosNormal.Left;
            this.Top = WndPosNormal.Top;
        }

        private void SaveConfig()
        {
            WndPosNormal = new Rect(this.Left, this.Top, this.Width, this.Height);

            Json.Value.Object json = new Json.Value.Object(new System.Collections.Generic.Dictionary<string, Json.Value>
            {
                { "width", WndPosNormal.Width },
                { "height", WndPosNormal.Height },
                { "left", WndPosNormal.Left },
                { "top", WndPosNormal.Top },
                { "width_attached", WndPosAttached.Width },
                { "height_attached", WndPosAttached.Height },
                { "left_attached", WndPosAttached.Left },
                { "top_attached", WndPosAttached.Top },
            });
            using (FileStream fileStream = new FileStream(ConfigFile, FileMode.Create, FileAccess.Write))
            {
                string jsonStr = json.ToString();
                byte[] bytes = Encoding.UTF8.GetBytes(jsonStr);
                fileStream.Write(bytes, 0, bytes.Length);
            }
        }

        private void InitRunePage()
        {
            NewRunePageItem newRunePageItem = new NewRunePageItem();
            newRunePageItem.Height *= uiScaleFactor;
            newRunePageItem.Width *= uiScaleFactor;
            newRunePageItem.Margin = new Thickness(newRunePageItem.Margin.Left * uiScaleFactor);
            newRunePageItem.StoreNew += NewRunePageItem_StoreNew;
            RunePagePanel.Children.Add(newRunePageItem);
        }

        private void LoadRepo()
        {
            if (!File.Exists(RepoFile))
            {
                Dispatcher.Invoke(() =>
                {
                    InitRunePage();
                });
                return;
            }
                
            using (StreamReader streamReader = new StreamReader(RepoFile))
            {
                string config = streamReader.ReadToEnd();
                Json.Value runePageArray = Json.Parser.Parse(config);

                Dispatcher.Invoke(() =>
                {
                    InitRunePage();
                });

                foreach (Json.Value value in runePageArray)
                {
                    Dispatcher.Invoke(() =>
                    {
                        AppendRunePage(value);
                    });
                }
            }
        }

        private void PhaseMonitor_PhaseChanged(object sender, GameflowPhaseMonitor.PhaseChangedArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if(e.NewPhase == "ChampSelect")
                {
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                }
                else if(e.OldPhase == "ChampSelect")
                {
                    this.WindowState = WindowState.Minimized;
                }
            });
        }

        private async void NewRunePageItem_StoreNew()
        {
            try
            {
                Json.Value value = await Wrapper.GetCurrentRunePageAsync();
                if (value != null)
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
                else
                {
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
            catch (RequestWrapper.NoClientException)
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

        private void AppendRunePage(Json.Value value)
        {
            RunePageItem runePageItem = new RunePageItem(value);
            runePageItem.Height *= uiScaleFactor;
            runePageItem.Width *= uiScaleFactor;
            runePageItem.Margin = new Thickness(runePageItem.Margin.Left * uiScaleFactor);
            runePageItem.Apply += RunePageItem_Apply;
            runePageItem.Delete += RunePageItem_Delete;
            runePageItem.Updated += RunePageItem_Updated;
            runePageItem.MouseMove += RunePageItem_MouseMove;
            RunePagePanel.Children.Insert(RunePagePanel.Children.Count - 1, runePageItem);

            Border border = new Border() { Height = runePageItem.Height, Width = runePageItem.Width, Margin = runePageItem.Margin, Background = new SolidColorBrush(Colors.Black)};
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
            try
            {
                Json.Value currentPageJson = await Wrapper.GetCurrentRunePageAsync();
                int order = 0;
                if (currentPageJson != null)
                {
                    ulong selectedId = currentPageJson["id"];
                    if (currentPageJson["isDeletable"])
                        await Wrapper.DeleteRunePageAsync(selectedId);
                }
                value["order"] = order;
                if (!await Wrapper.AddRunePageAsync(value))
                {
                    Json.Value pages = await Wrapper.GetRunePagesAsync();
                    foreach (Json.Value pageJson in pages)
                    {
                        if (pageJson["isDeletable"])
                        {
                            value["order"] = pageJson["order"];
                            await Wrapper.DeleteRunePageAsync(pageJson["id"]);
                            await Wrapper.AddRunePageAsync(value);
                            break;
                        }
                    }
                }
            }
            catch (RequestWrapper.NoClientException)
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

            using (StreamWriter streamWriter = new StreamWriter(RepoFile))
            {
                streamWriter.Write(runePageArray.ToString());
            }
        }

        private void CloseWindowBtn_Clicked(object sender, MouseButtonEventArgs e)
        {
            if (AttachCore.IsAttached)
            {
                AttachCore.Detach();
                Widget.Detach();
                Widget.Close();
                Widget = null;
            }
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



        WidgetWindow Widget;

        private void SetDetach()
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            uint dwStyle = Native.GetWindowLong(hwnd, (int)Native.WindowLongFlags.GWL_STYLE);
            dwStyle &= ~(uint)Native.WindowStyles.WS_CHILDWINDOW;
            dwStyle |= (uint)Native.WindowStyles.WS_CAPTION;
            Native.SetWindowLong(hwnd, (int)Native.WindowLongFlags.GWL_STYLE, dwStyle);

            WndPosAttached = new Rect(this.Left, this.Top, this.Width, this.Height);
            this.Width = WndPosNormal.Width;
            this.Height = WndPosNormal.Height;
            this.Left = WndPosNormal.Left;
            this.Top = WndPosNormal.Top;

            MaximizeWindowBtn.Visibility = Visibility.Visible;
        }

        private void SetAttach()
        {
            WndPosNormal = new Rect(this.Left, this.Top, this.Width, this.Height);
            this.Width = WndPosAttached.Width;
            this.Height = WndPosAttached.Height;
            this.Left = WndPosAttached.Left;
            this.Top = WndPosAttached.Top;

            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            uint dwStyle = Native.GetWindowLong(hwnd, (int)Native.WindowLongFlags.GWL_STYLE);
            dwStyle |= (uint)Native.WindowStyles.WS_CHILDWINDOW;
            dwStyle &= ~(uint)Native.WindowStyles.WS_CAPTION;
            Native.SetWindowLong(hwnd, (int)Native.WindowLongFlags.GWL_STYLE, dwStyle);

            MaximizeWindowBtn.Visibility = Visibility.Collapsed;
        }

        private void AttachWindowBtn_Clicked(object sender, MouseButtonEventArgs e)
        {
            if (AttachCore.IsAttached)
            {
                AttachCore.Detach();

                SetDetach();

                // widget

                // TODO : Weird behaviour when transparent window attached the second time. So hide it for now. 
                // Need restart the software to attach to a new client window
                Widget.Hide();
                //Widget.Close();
                //Widget = null;
            }
            else
            {
                SetAttach();

                if (AttachCore.AttachToClient(true))
                {
                    // widget
                    if (Widget == null)
                    {
                        Widget = new WidgetWindow(this);
                        Widget.TargetLostDetached += Widget_TargetLostDetached;
                        Widget.Show();

                        IntPtr hwndWidget = new WindowInteropHelper(Widget).Handle;
                        uint dwStyleWidget = Native.GetWindowLong(hwndWidget, (int)Native.WindowLongFlags.GWL_STYLE);
                        dwStyleWidget |= (uint)Native.WindowStyles.WS_CHILDWINDOW;
                        Native.SetWindowLong(hwndWidget, (int)Native.WindowLongFlags.GWL_STYLE, dwStyleWidget);

                        Widget.AttachToClient();
                        AttachCore.AttachToClient();
                    }
                    else
                    {
                        Widget.Show();
                    }
                }
                else
                {
                    SetDetach();
                }
            }

        }

        private void AttachCore_TargetLostDetached(object sender, EventArgs e)
        {
            // TODO : Weird behaviour when transparent window attached the second time. So exit when current attached client lost.

            //Widget.Close();
            //Widget = null;

            //SetDetach();
        }

        private void Widget_TargetLostDetached(object sender, EventArgs e)
        {
            // TODO : Weird behaviour when transparent window attached the second time. So exit when current attached client lost.
            this.Close();
        }

    }
}
