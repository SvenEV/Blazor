using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Math;

namespace StandaloneApp.UI
{
    public class UIElement : BlazorComponent
    {
        public string LayoutCss => $"position: absolute; left: {Bounds.X}px; top: {Bounds.Y}px; width: {Bounds.Width}px; height: {Bounds.Height}px; ";

        public RenderFragment ChildContent { get; set; }

        public double Width { get; set; } = double.NaN;

        public double Height { get; set; } = double.NaN;

        public Thickness Margin { get; set; } = Thickness.Zero;

        public Alignment HorizontalAlignment { get; set; } = Alignment.Stretch;

        public Alignment VerticalAlignment { get; set; } = Alignment.Stretch;

        public Point MinSize { get; set; } = Point.Zero;

        public Point MaxSize { get; set; } = Point.PositiveInfinity;

        public Point DesiredSize { get; private set; }

        public Rect Bounds { get; private set; }

        public IEnumerable<UIElement> Children => this.GetChildren()?.OfType<UIElement>() ?? Enumerable.Empty<UIElement>();

        public UIElement()
        {
            this.SetChildrenChangedHandler(_ => XamzorView.Current?.Layout());
        }
        
        public void Measure(Point availableSize)
        {
            if (double.IsNaN(availableSize.X) || double.IsNaN(availableSize.Y))
                throw new ArgumentException("NaN is invalid for Measure", nameof(availableSize));

            DesiredSize = Point.Min(MeasureCore(availableSize), availableSize);
            StateHasChanged();
        }

        public void Arrange(Rect finalRect)
        {
            if (IsInvalidRect(finalRect))
                throw new ArgumentException($"Invalid Arrange rectangle: {finalRect}", nameof(finalRect));

            ArrangeCore(finalRect);
            StateHasChanged();
        }

        private Point MeasureCore(Point availableSize)
        {
            var constrainedSize = Point.Clamp(availableSize - Margin.Size, MinSize, MaxSize);
            var measuredSize = MeasureOverride(constrainedSize);

            measuredSize = new Point(
                Width.OrIfNan(measuredSize.X),
                Height.OrIfNan(measuredSize.Y));

            measuredSize = Point.Clamp(measuredSize, MinSize, MaxSize);
            return Point.Max(Point.Zero, measuredSize + Margin.Size);
        }

        private void ArrangeCore(Rect finalRect)
        {
            var origin = finalRect.TopLeft + new Point(Margin.Left, Margin.Top);
            var availableSizeMinusMargins = Point.Max(Point.Zero, finalRect.Size - Margin.Size);
            var size = availableSizeMinusMargins;

            if (HorizontalAlignment != Alignment.Stretch)
                size = size.WithX(Min(size.X, DesiredSize.X - Margin.HorizontalThickness));

            if (VerticalAlignment != Alignment.Stretch)
                size = size.WithY(Min(size.Y, DesiredSize.Y - Margin.VerticalThickness));

            size = Point.Clamp(size, MinSize, MaxSize);
            size = Point.Min(ArrangeOverride(size), size);

            switch (HorizontalAlignment)
            {
                case Alignment.Center:
                case Alignment.Stretch:
                    origin += new Point((availableSizeMinusMargins.X - size.X) / 2, 0);
                    break;

                case Alignment.End:
                    origin += new Point(availableSizeMinusMargins.X - size.X, 0);
                    break;
            }

            switch (VerticalAlignment)
            {
                case Alignment.Center:
                case Alignment.Stretch:
                    origin += new Point(0, (availableSizeMinusMargins.Y - size.Y) / 2);
                    break;

                case Alignment.End:
                    origin += new Point(0, availableSizeMinusMargins.Y - size.Y);
                    break;
            }

            Bounds = new Rect(origin, size);
        }

        private bool IsInvalidRect(Rect rect) => 
            rect.Width < 0 || rect.Height < 0 ||
            double.IsInfinity(rect.X) || double.IsInfinity(rect.Y) ||
            double.IsInfinity(rect.Width) || double.IsInfinity(rect.Height) ||
            double.IsNaN(rect.X) || double.IsNaN(rect.Y) ||
            double.IsNaN(rect.Width) || double.IsNaN(rect.Height);

        protected virtual Point MeasureOverride(Point availableSize)
        {
            var size = Point.Zero;

            foreach (var child in Children)
            {
                child.Measure(availableSize);
                size = Point.Max(size, child.DesiredSize);
            }

            return size;
        }

        protected virtual Point ArrangeOverride(Point finalSize)
        {
            foreach (var child in Children)
                child.Arrange(new Rect(Point.Zero, finalSize));

            return finalSize;
        }
    }

    public class LayoutProps
    {
        
    }
}
