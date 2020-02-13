using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RuneRepo
{
    class SmoothScrollGrid : Grid, IScrollInfo
    {
        TranslateTransform _transForm;
        public SmoothScrollGrid()
        {
            _transForm = new TranslateTransform();
            this.RenderTransform = _transForm;
        }

        #region Layout

        Size _screenSize;
        Size _totalSize;

        protected override Size MeasureOverride(Size availableSize)
        {
            _screenSize = availableSize;

            Size constraint = new Size(double.PositiveInfinity, double.PositiveInfinity);
            if (ScrollOwner != null)
            {
                if (CanVerticallyScroll)
                    constraint.Width = availableSize.Width;
                if (CanHorizontallyScroll)
                    constraint.Height = availableSize.Height;
            }
            
            _totalSize = base.MeasureOverride(constraint);

            appendOffset(0, 0);

            return _totalSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var size = base.ArrangeOverride(finalSize);
            if (ScrollOwner != null)
            {
                var yOffsetAnimation = new DoubleAnimation() { To = -VerticalOffset, Duration = TimeSpan.FromSeconds(0.2), EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut } };
                _transForm.BeginAnimation(TranslateTransform.YProperty, yOffsetAnimation);

                var xOffsetAnimation = new DoubleAnimation() { To = -HorizontalOffset, Duration = TimeSpan.FromSeconds(0.2), EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut } };
                _transForm.BeginAnimation(TranslateTransform.XProperty, xOffsetAnimation);

                ScrollOwner.InvalidateScrollInfo();
            }
            return _screenSize;
        }
        #endregion

        #region IScrollInfo

        public ScrollViewer ScrollOwner { get; set; }
        public bool CanHorizontallyScroll { get; set; }
        public bool CanVerticallyScroll { get; set; }

        public double ExtentHeight { get { return _totalSize.Height; } }
        public double ExtentWidth { get { return _totalSize.Width; } }

        public double HorizontalOffset { get; private set; }
        public double VerticalOffset { get; private set; }

        public double ViewportHeight { get { return _screenSize.Height; } }
        public double ViewportWidth { get { return _screenSize.Width; } }

        void appendOffset(double x, double y)
        {
            var offset = new Vector(HorizontalOffset + x, VerticalOffset + y);

            offset.Y = range(offset.Y, 0, _totalSize.Height - _screenSize.Height);
            offset.X = range(offset.X, 0, _totalSize.Width - _screenSize.Width);

            HorizontalOffset = offset.X;
            VerticalOffset = offset.Y;

            InvalidateArrange();
        }

        double range(double value, double value1, double value2)
        {
            var min = Math.Min(value1, value2);
            var max = Math.Max(value1, value2);

            value = Math.Max(value, min);
            value = Math.Min(value, max);

            return value;
        }


        const double _lineOffset = 30;
        const double _wheelOffset = 90;

        public void LineDown()
        {
            appendOffset(0, _lineOffset);
        }

        public void LineUp()
        {
            appendOffset(0, -_lineOffset);
        }

        public void LineLeft()
        {
            appendOffset(-_lineOffset, 0);
        }

        public void LineRight()
        {
            appendOffset(_lineOffset, 0);
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            throw new NotSupportedException();
        }

        public void MouseWheelDown()
        {
            appendOffset(0, _wheelOffset);
        }

        public void MouseWheelUp()
        {
            appendOffset(0, -_wheelOffset);
        }

        public void MouseWheelLeft()
        {
            appendOffset(0, _wheelOffset);
        }

        public void MouseWheelRight()
        {
            appendOffset(_wheelOffset, 0);
        }

        public void PageDown()
        {
            appendOffset(0, _screenSize.Height);
        }

        public void PageUp()
        {
            appendOffset(0, -_screenSize.Height);
        }

        public void PageLeft()
        {
            appendOffset(-_screenSize.Width, 0);
        }

        public void PageRight()
        {
            appendOffset(_screenSize.Width, 0);
        }

        public void SetVerticalOffset(double offset)
        {
            this.appendOffset(HorizontalOffset, offset - VerticalOffset);
        }

        public void SetHorizontalOffset(double offset)
        {
            this.appendOffset(offset - HorizontalOffset, VerticalOffset);
        }
        #endregion
    }
}
