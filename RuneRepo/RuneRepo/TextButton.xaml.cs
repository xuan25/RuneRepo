using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RuneRepo
{
    /// <summary>
    /// Interaction logic for TextButton.xaml
    /// </summary>
    public partial class TextButton : UserControl
    {
        public delegate void ClickedHendler(object sender, MouseButtonEventArgs e);
        public event ClickedHendler Clicked;

        public string Text
        {
            get
            {
                return (string)GetValue(TextProperty);
            }
            set
            {
                SetValue(TextProperty, value);
            }
        }
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(TextButton), new FrameworkPropertyMetadata(null));
        readonly DependencyPropertyDescriptor TextPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(TextProperty, typeof(TextButton));

        public TextButton()
        {
            InitializeComponent();
            TextPropertyDescriptor.AddValueChanged(this, TextChanged);
        }

        private void TextChanged(object sender, EventArgs e)
        {
            ButtonTextBox.Text = Text;
        }

        private void ButtonGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ((Grid)sender).CaptureMouse();
            PressedBorder.Visibility = Visibility.Visible;
            ButtonTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5c5b57"));
        }

        private void ButtonGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ((Grid)sender).ReleaseMouseCapture();
            PressedBorder.Visibility = Visibility.Hidden;
            ButtonTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#cdbe91"));

            if (this.IsMouseOver)
                Clicked?.Invoke(this, e);
        }

        private void ButtonGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Released)
            {
                PressedBorder.Visibility = Visibility.Hidden;
                ButtonTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#cdbe91"));
            }
        }
    }
}
