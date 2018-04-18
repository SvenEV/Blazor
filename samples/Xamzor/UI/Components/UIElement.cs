using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;
using System;
using System.Collections.Generic;

namespace Xamzor.UI.Components
{
    public class UIElement : BlazorComponent, IDisposable
    {
        private readonly UIElementConfiguration _config;
        private ApplicationView _view;
        private int? _measureHash;
        private int? _arrangeHash;

        public bool IsMeasureValid { get; private set; } = false;
        public bool IsArrangeValid { get; private set; } = false;
        public  Point? PreviousMeasureInput { get; private set; }
        public  Rect? PreviousArrangeInput { get; private set; }

        public string LayoutCss =>
            $"position: absolute; overflow: hidden; " +
            $"left: {Bounds.X}px; top: {Bounds.Y}px; width: {Bounds.Width}px; height: {Bounds.Height}px; " +
            $"clip: rect({ClippedBounds.Y - Bounds.Y}px, {ClippedBounds.X - Bounds.X + ClippedBounds.Width}px, {ClippedBounds.Y - Bounds.Y + ClippedBounds.Height}px, {ClippedBounds.X - Bounds.X}px); ";

        public string Id { get; }

        public ApplicationView View
        {
            get => _view ?? throw new InvalidOperationException($"Not attached to a view ({this})");
            private set => _view = value;
        }

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

        public Rect ClippedBounds { get; private set; } // bounds clipped to the finalRect size

        // Temporary properties - we'll invent some form of "attached properties" in the future
        public int Row { get; set; } = 0;
        public int Column { get; set; } = 0;
        public int RowSpan { get; set; } = 1;
        public int ColumnSpan { get; set; } = 1;

        public UIElement()
        {
            Id = GetType().Name + "_" + Guid.NewGuid().ToString();
            _config = new UIElementConfiguration();
            OnConfiguring(_config);
        }

        protected virtual UIElementConfiguration OnConfiguring(UIElementConfiguration config) => config
            .AffectsMeasure(nameof(ChildContent))
            .AffectsMeasure(nameof(Margin))
            .AffectsMeasure(nameof(Column))
            .AffectsMeasure(nameof(Row))
            .AffectsMeasure(nameof(Width))
            .AffectsMeasure(nameof(Height))
            .AffectsMeasure(nameof(MinWidth))
            .AffectsMeasure(nameof(MinHeight))
            .AffectsMeasure(nameof(MaxWidth))
            .AffectsMeasure(nameof(MaxHeight))
            .AffectsMeasure(nameof(HorizontalAlignment))
            .AffectsMeasure(nameof(VerticalAlignment));

        protected override void OnInit()
        {
            if (!(this is XamzorView))
                Application.RegisterElement(this);

            UILog.Write("INIT", GetType().Name + " " + Tag + " initialized");
        }

        public void AttachToView(ApplicationView view) => View = view;

        public void DetachFromView() => View = View;

        public override void SetParameters(ParameterCollection parameters)
        {
            base.SetParameters(parameters);

            var newMeasureHash = 832753926;
            var newArrangeHash = 832753926;

            foreach (var p in parameters)
            {
                var affectsMeasure = _config.PropertiesAffectingMeasure.Contains(p.Name);
                var affectsArrange = _config.PropertiesAffectingArrange.Contains(p.Name);

                if (affectsMeasure)
                    newMeasureHash = newMeasureHash * -1521134295 + (p.Value?.GetHashCode() ?? 0);

                if (affectsArrange)
                    newArrangeHash = newArrangeHash * -1521134295 + (p.Value?.GetHashCode() ?? 0);
            }

            if (newMeasureHash != _measureHash)
            {
                IsMeasureValid = false;
                IsArrangeValid = false;
                LayoutManager.Instance.InvalidateMeasure(this);
            }
            else if (newArrangeHash != _arrangeHash)
            {
                IsArrangeValid = false;
                LayoutManager.Instance.InvalidateArrange(this);
            }

            _measureHash = newMeasureHash;
            _arrangeHash = newArrangeHash;

            InvalidateMeasure();
        }
        
        public void InvalidateMeasure()
        {
            if (IsMeasureValid)
            {
                IsMeasureValid = false;
                IsArrangeValid = false;
                LayoutManager.Instance.InvalidateMeasure(this);
            }
        }

        public void InvalidateArrange()
        {
            if (IsArrangeValid)
            {
                IsArrangeValid = false;
                LayoutManager.Instance.InvalidateArrange(this);
            }
        }

        public Point Measure(Point availableSize)
        {
            if (IsInvalidInput(availableSize))
                throw new LayoutException($"Invalid input for '{GetType().Name}.Measure': {availableSize}");

            // If possible, use cached desired size
            if (IsMeasureValid && PreviousMeasureInput == availableSize)
            {
                UILog.Write("DEBUG", $"Using cached DesiredSize of '{this}'");
                return DesiredSize;
            }

            using (UILog.BeginScope("LAYOUT",
                $"{GetType().Name}.Measure{availableSize}...",
                () => $"<<< {GetType().Name}.{nameof(DesiredSize)} = {DesiredSize}"))
            {
                DesiredSize = Point.Min(MeasureCore(availableSize), availableSize);
                PreviousMeasureInput = availableSize;
                IsMeasureValid = true;
            }

            if (IsInvalidOutput(DesiredSize))
                throw new LayoutException($"Invalid result from '{GetType().Name}.Measure({availableSize})': {DesiredSize}");

            return DesiredSize;

            // Available size must not be NaN or negative (but can be infinity)
            bool IsInvalidInput(Point size) =>
                size.X < 0 || size.Y < 0 ||
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

            if (!IsMeasureValid)
                Measure(PreviousMeasureInput ?? finalRect.Size);

            // If possible, use cached rect
            if (IsArrangeValid && PreviousArrangeInput == finalRect)
            {
                UILog.Write("DEBUG", $"Using cached Bounds of '{this}'");
                return Bounds;
            }

            using (UILog.BeginScope("LAYOUT",
                $"{GetType().Name}.Arrange{finalRect}...",
                () => $"<<< {GetType().Name}.{nameof(Bounds)} = {Bounds}"))
            {
                Bounds = ArrangeCore(finalRect);
                ClippedBounds = finalRect;
                PreviousArrangeInput = finalRect;
                IsArrangeValid = true;
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

            foreach (var child in this.Children())
            {
                child.Measure(availableSize);
                size = Point.Max(size, child.DesiredSize);
            }

            return size;
        }

        protected virtual Point ArrangeOverride(Point finalSize)
        {
            // By default: Position all children at (0, 0)
            foreach (var child in this.Children())
                child.Arrange(new Rect(Point.Zero, finalSize));

            return finalSize;
        }

        public override string ToString() => GetType().Name + " " + Tag;

        public virtual void Dispose()
        {
            UILog.Write("DISPOSE", "Disposed " + this);

            if (!(this is XamzorView))
                Application.UnregisterElement(this);
        }
    }
}

public class UIElementConfiguration
{
    private readonly HashSet<string> _propertiesAffectingMeasure = new HashSet<string>();
    private readonly HashSet<string> _propertiesAffectingArrange = new HashSet<string>();

    // TODO: need some kind of IReadOnlyCollection with Contains(...) here
    public HashSet<string> PropertiesAffectingMeasure => _propertiesAffectingMeasure;

    public HashSet<string> PropertiesAffectingArrange => _propertiesAffectingArrange;

    public UIElementConfiguration AffectsMeasure(string propertyName)
    {
        _propertiesAffectingMeasure.Add(propertyName);
        return this;
    }

    public UIElementConfiguration AffectsArrange(string propertyName)
    {
        _propertiesAffectingArrange.Add(propertyName);
        return this;
    }
}