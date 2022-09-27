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
using System.Windows.Media.Imaging;

namespace RuneRepo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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

        public static readonly DependencyProperty RepoControlProperty = DependencyProperty.Register(
            "RepoControl", typeof(RuneRepoControl),
            typeof(MainWindow)
            );

        public RuneRepoControl RepoControl
        {
            get => (RuneRepoControl)GetValue(RepoControlProperty);
            set => SetValue(RepoControlProperty, value);
        }

        public static readonly DependencyProperty BackgroundImageProperty = DependencyProperty.Register(
            "BackgroundImage", typeof(BitmapSource),
            typeof(MainWindow)
            );

        public BitmapSource BackgroundImage
        {
            get => (BitmapSource)GetValue(BackgroundImageProperty);
            set => SetValue(BackgroundImageProperty, value);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    BitmapImage background = new BitmapImage();
                    background.BeginInit();
                    background.UriSource = new Uri("pack://application:,,,/images/backdrop-magic.png", UriKind.RelativeOrAbsolute);
                    background.EndInit();
                    BackgroundImage = background;
                }, System.Windows.Threading.DispatcherPriority.Loaded);

                Wrapper = new RequestWrapper();
                PhaseMonitor = new GameflowPhaseMonitor(Wrapper);
                PhaseMonitor.PhaseChanged += PhaseMonitor_PhaseChanged;
                PhaseMonitor.Start();

                RuneRepoControl repoControl = null;
                Dispatcher.Invoke(() =>
                {
                    repoControl = new RuneRepoControl(Wrapper);
                    RepoControl = repoControl;
                });

                repoControl.LoadRepo();
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

        private void PhaseMonitor_PhaseChanged(object sender, GameflowPhaseMonitor.PhaseChangedArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.NewPhase == "ChampSelect")
                {
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                }
                else if (e.OldPhase == "ChampSelect")
                {
                    this.WindowState = WindowState.Minimized;
                }
            });
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
