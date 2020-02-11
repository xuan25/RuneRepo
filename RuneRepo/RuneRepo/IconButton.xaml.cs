using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RuneRepo
{
    /// <summary>
    /// Interaction logic for IconButton.xaml
    /// </summary>
    public partial class IconButton : UserControl
    {
        public delegate void ClickedHendler(object sender, MouseButtonEventArgs e);
        public event ClickedHendler Clicked;

        public Geometry IconGeometry
        {
            get
            {
                return (Geometry)GetValue(IconGeometryProperty);
            }
            set
            {
                SetValue(IconGeometryProperty, value);
            }
        }
        public static readonly DependencyProperty IconGeometryProperty = DependencyProperty.Register("IconGeometry", typeof(Geometry), typeof(IconButton), new FrameworkPropertyMetadata(null));
        readonly DependencyPropertyDescriptor IconGeometryPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(IconGeometryProperty, typeof(IconButton));

        public IconButton()
        {
            InitializeComponent();

            IconGeometryPropertyDescriptor.AddValueChanged(this, IconGeometryChanged);
            this.DataContext = this;
        }

        private void IconGeometryChanged(object sender, EventArgs e)
        {
            IconPath.Data = IconGeometry;
            IconPathPressed.Data = IconGeometry;
        }

        private void ButtonGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ((Grid)sender).CaptureMouse();
            IconPathPressed.Visibility = Visibility.Visible;
            //IconPath.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#785a28"));
        }

        private void ButtonGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ((Grid)sender).ReleaseMouseCapture();
            IconPathPressed.Visibility = Visibility.Hidden;
            //IconPath.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ccbd90"));

            if (this.IsMouseOver)
                Clicked?.Invoke(this, e);
        }
    }
}
