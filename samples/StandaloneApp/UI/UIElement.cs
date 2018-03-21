using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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

        public Point DesiredSize { get; private set; } // excluding margins

        public Rect Bounds { get; private set; } // size is excluding margins

        public IEnumerable<UIElement> Children => this.GetChildren()?.OfType<UIElement>() ?? Enumerable.Empty<UIElement>();

        public UIElement()
        {
            this.SetChildrenChangedHandler(_ => XamzorView.Current?.Layout());
        }

        public Point Measure(Point availableSize)
        {
            if (IsInvalidInput(availableSize))
                throw new LayoutException($"Invalid input for '{GetType().Name}.Measure': {availableSize}");

            DesiredSize = Point.Min(MeasureCore(availableSize), availableSize);

            if (IsInvalidOutput(DesiredSize))
                throw new LayoutException($"Invalid result from '{GetType().Name}.Measure({availableSize})': {DesiredSize}");

            return DesiredSize;

            // Available size must not be NaN (but can be infinity)
            bool IsInvalidInput(Point size) =>
                double.IsNaN(size.X) || double.IsNaN(size.Y);

            // Desired size must be >=0 (not NaN and not infinity)
            bool IsInvalidOutput(Point size) =>
                size.X < 0 || size.Y < 0 ||
                double.IsInfinity(size.X) || double.IsInfinity(size.Y) ||
                double.IsNaN(size.X) || double.IsNaN(size.Y);
        }

        public Rect Arrange(Rect finalRect)
        {
            if (IsInvalidInput(finalRect))
                throw new LayoutException($"Invalid input for '{GetType().Name}.Arrange': {finalRect}");

            Bounds = ArrangeCore(finalRect);
            StateHasChanged();
            return Bounds;

            // Position and size must not be NaN or infinity, and size must be >=0
            bool IsInvalidInput(Rect rect) =>
                rect.Width < 0 || rect.Height < 0 ||
                double.IsInfinity(rect.X) || double.IsInfinity(rect.Y) ||
                double.IsInfinity(rect.Width) || double.IsInfinity(rect.Height) ||
                double.IsNaN(rect.X) || double.IsNaN(rect.Y) ||
                double.IsNaN(rect.Width) || double.IsNaN(rect.Height);
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

        private Rect ArrangeCore(Rect finalRect)
        {
            var availableSizeMinusMargins = Point.Max(Point.Zero, finalRect.Size - Margin.Size);

            // Calculate used size
            var size = new Point(
                HorizontalAlignment == Alignment.Stretch ? availableSizeMinusMargins.X : Min(availableSizeMinusMargins.X, DesiredSize.X - Margin.HorizontalThickness),
                VerticalAlignment == Alignment.Stretch ? availableSizeMinusMargins.Y : Min(availableSizeMinusMargins.Y, DesiredSize.Y - Margin.VerticalThickness));

            size = Point.Clamp(size, MinSize, MaxSize);
            size = Point.Min(ArrangeOverride(size), size);

            // Calculate offset
            var origin = finalRect.TopLeft + Margin.TopLeft;

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

            return new Rect(origin, size);
        }

        /// <param name="availableSize">Size available for the element, excluding margin</param>
        protected virtual Point MeasureOverride(Point availableSize)
        {
            // By default: Return bounding box size of all children positioned at (0, 0)
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
            // By default: Position all children at (0, 0)
            foreach (var child in Children)
                child.Arrange(new Rect(Point.Zero, finalSize));

            return finalSize;
        }
    }
}
