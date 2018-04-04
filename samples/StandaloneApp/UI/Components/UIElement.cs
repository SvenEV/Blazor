using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;
using System.Collections.Generic;
using System.Linq;

namespace StandaloneApp.UI.Components
{
    public class UIElement : BlazorComponent
    {
        public string LayoutCss => $"position: absolute; overflow: hidden; left: {Bounds.X}px; top: {Bounds.Y}px; width: {Bounds.Width}px; height: {Bounds.Height}px; ";

        public RenderFragment ChildContent { get; set; }

        public double Width { get; set; } = double.NaN;

        public double Height { get; set; } = double.NaN;

        public double MinWidth { get; set; } = 0;

        public double MinHeight { get; set; } = 0;

        public double MaxWidth { get; set; } = double.PositiveInfinity;

        public double MaxHeight { get; set; } = double.PositiveInfinity;

        public Point Size => new Point(Width, Height);

        public Point MinSize => new Point(MinWidth, MinHeight);

        public Point MaxSize => new Point(MaxWidth, MaxHeight);

        public Thickness Margin { get; set; } = Thickness.Zero;

        public string Tag { get; set; }
        
        public Alignment HorizontalAlignment { get; set; } = Alignment.Stretch;

        public Alignment VerticalAlignment { get; set; } = Alignment.Stretch;

        public Point DesiredSize { get; private set; } // includes margins, computed by Measure()

        public Rect Bounds { get; private set; } // size excludes margins, computed by Arrange()

        public UIElement Parent => RenderHandle.GetParent() as UIElement;

        public IEnumerable<UIElement> Children => RenderHandle.GetChildren().OfType<UIElement>();

        // Temporary properties - we'll invent some form of "attached properties" in the future
        public int Row { get; set; } = 0;
        public int Column { get; set; } = 0;
        public int RowSpan { get; set; } = 1;
        public int ColumnSpan { get; set; } = 1;

        protected override void OnInit()
        {
            UILog.Write("INIT", GetType().Name + " " + Tag + " initialized");
            RenderHandle.ChildrenChanged += _ => RecalculateLayout();
            RecalculateLayout();
        }

        public Point Measure(Point availableSize)
        {
            if (!RenderHandle.IsInitialized)
            {
                UILog.Write("LAYOUT", $"{GetType().Name}.Measure({availableSize}) returned early - RenderHandle not initialized");
                return Point.Zero;
            }

            if (IsInvalidInput(availableSize))
                throw new LayoutException($"Invalid input for '{GetType().Name}.Measure': {availableSize}");

            using (UILog.BeginScope("LAYOUT",
                $"{GetType().Name}.Measure({availableSize})...",
                () => $"<<< {nameof(DesiredSize)} = {DesiredSize}"))
            {
                DesiredSize = Point.Min(MeasureCore(availableSize), availableSize);
            }

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
            if (!RenderHandle.IsInitialized)
                return default;

            if (IsInvalidInput(finalRect))
                throw new LayoutException($"Invalid input for '{GetType().Name}.Arrange': {finalRect}");

            using (UILog.BeginScope("LAYOUT",
                $"{GetType().Name}.Arrange({finalRect})...",
                () => $"<<< {nameof(Bounds)} = {Bounds}"))
            {
                Bounds = ArrangeCore(finalRect);
            }
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
            // Effective min/max size accounts for explicitly set Width/Height
            var effectiveMinSize = Point.Clamp(Size.OrWhereNaN(Point.Zero), MinSize, MaxSize);
            var effectiveMaxSize = Point.Clamp(Size.OrWhereNaN(Point.PositiveInfinity), MinSize, MaxSize);
            var constrainedSize = Point.Clamp(availableSize - Margin.Size, effectiveMinSize, effectiveMaxSize);

            var measuredSize = MeasureOverride(constrainedSize);
            var desiredSize = Point.Clamp(Size.OrWhereNaN(measuredSize), MinSize, MaxSize);
            return Point.Max(Point.Zero, desiredSize + Margin.Size);
        }

        private Rect ArrangeCore(Rect finalRect)
        {
            var availableSizeMinusMargins = Point.Max(Point.Zero, finalRect.Size - Margin.Size);
            var finalSize = ComputeSize();
            var finalOffset = ComputeOffset(finalSize);
            return new Rect(finalOffset, finalSize);

            Point ComputeSize()
            {
                // On 'Stretch' start with full available size, otherwise start with DesiredSize
                var arrangeSize = new Point(
                    HorizontalAlignment == Alignment.Stretch ? availableSizeMinusMargins.X : DesiredSize.X,
                    VerticalAlignment == Alignment.Stretch ? availableSizeMinusMargins.Y : DesiredSize.Y);

                // Effective min/max size accounts for explicitly set Width/Height
                var effectiveMinSize = Point.Clamp(Size.OrWhereNaN(Point.Zero), MinSize, MaxSize);
                var effectiveMaxSize = Point.Clamp(Size.OrWhereNaN(Point.PositiveInfinity), MinSize, MaxSize);
                arrangeSize = Point.Clamp(arrangeSize, effectiveMinSize, effectiveMaxSize);

                // Note: Returned size may exceed available size so that content gets clipped
                return Point.Clamp(ArrangeOverride(arrangeSize), effectiveMinSize, effectiveMaxSize);
            }

            Point ComputeOffset(Point size)
            {
                // If size returned by ArrangeOverride() exceeds available space,
                // content will be clipped and we fall back to top/left alignment
                var effectiveHAlign = (HorizontalAlignment == Alignment.Stretch && size.X > availableSizeMinusMargins.X)
                    ? Alignment.Start
                    : HorizontalAlignment;

                var effectiveVAlign = (VerticalAlignment == Alignment.Stretch && size.Y > availableSizeMinusMargins.Y)
                    ? Alignment.Start
                    : VerticalAlignment;

                var offset = finalRect.TopLeft + Margin.TopLeft;

                switch (effectiveHAlign)
                {
                    case Alignment.Center:
                    case Alignment.Stretch:
                        offset += new Point((availableSizeMinusMargins.X - size.X) / 2, 0);
                        break;

                    case Alignment.End:
                        offset += new Point(availableSizeMinusMargins.X - size.X, 0);
                        break;
                }

                switch (effectiveVAlign)
                {
                    case Alignment.Center:
                    case Alignment.Stretch:
                        offset += new Point(0, (availableSizeMinusMargins.Y - size.Y) / 2);
                        break;

                    case Alignment.End:
                        offset += new Point(0, availableSizeMinusMargins.Y - size.Y);
                        break;
                }

                return offset;
            }
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

        protected void RecalculateLayout()
        {
            // Temporary solution to force a full recalculation of the XamzorView layout
            var current = this;
            while (current.Parent != null)
                current = current.Parent;

            if (current is XamzorView root)
                root.Layout();
        }

        public override string ToString() => GetType().Name + " " + Tag;
    }
}
