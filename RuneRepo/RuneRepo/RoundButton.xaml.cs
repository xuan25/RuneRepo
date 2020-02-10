using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RuneRepo
{
    /// <summary>
    /// Interaction logic for DeleteButton.xaml
    /// </summary>
    public partial class RoundButton : UserControl
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
        public static readonly DependencyProperty IconGeometryProperty = DependencyProperty.Register("IconGeometry", typeof(Geometry), typeof(RoundButton), new FrameworkPropertyMetadata(null));
        readonly DependencyPropertyDescriptor IconGeometryPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(IconGeometryProperty, typeof(RoundButton));

        public RoundButton()
        {
            InitializeComponent();
            IconGeometryPropertyDescriptor.AddValueChanged(this, IconGeometryChanged);
        }

        private void IconGeometryChanged(object sender, EventArgs e)
        {
            IconPath.Data = IconGeometry;
        }

        private void ButtonGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ((Grid)sender).CaptureMouse();
            PressedBorder.Visibility = Visibility.Visible;
            IconPath.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#785a28"));
        }

        private void ButtonGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ((Grid)sender).ReleaseMouseCapture();
            PressedBorder.Visibility = Visibility.Hidden;
            IconPath.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ccbd90"));

            if (this.IsMouseOver)
                Clicked?.Invoke(this, e);
        }
    }
}
