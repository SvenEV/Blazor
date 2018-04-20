﻿using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xamzor.UI.Components
{
    public class UIElement : BlazorComponent, IDisposable
    {
        private ApplicationView _view;

        public string LayoutCss { get; private set; }

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

        public Thickness Padding { get; set; } = Thickness.Zero;

        public string Tag { get; set; }

        public Alignment HorizontalAlignment { get; set; } = Alignment.Stretch;

        public Alignment VerticalAlignment { get; set; } = Alignment.Stretch;

        // Temporary properties - we'll invent some form of "attached properties" in the future
        public int Row { get; set; } = 0;
        public int Column { get; set; } = 0;
        public int RowSpan { get; set; } = 1;
        public int ColumnSpan { get; set; } = 1;

        private string AlignmentCss(Alignment alignment) =>
            alignment == Alignment.Start ? "flex-start" :
            alignment == Alignment.Center ? "center" :
            alignment == Alignment.End ? "flex-end" :
            "stretch";

        public UIElement()
        {
            Id = GetType().Name + "_" + Guid.NewGuid().ToString();
        }

        protected override void OnInit()
        {
            if (!(this is XamzorView))
                Application.RegisterElement(this);

            UILog.Write("INIT", GetType().Name + " " + Tag + " initialized");
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            UpdateLayoutCss();
        }

        public void AttachToView(ApplicationView view)
        {
            View = view;
            StateHasChanged();
        }

        public void DetachFromView() => View = View;

        public override string ToString() => GetType().Name + " " + Tag;

        public virtual void Dispose()
        {
            UILog.Write("DISPOSE", "Disposed " + this);

            if (!(this is XamzorView))
                Application.UnregisterElement(this);
        }

        private void UpdateLayoutCss()
        {
            var sb = new StringBuilder();

            ComputeOwnLayoutCss(sb);

            var parent = _view?.VisualTree.Parent(this);
            UILog.Write("DEBUG", $"({parent}).ComputeChildLayout({this})");
            parent?.ComputeChildLayoutCss(sb, this);

            LayoutCss = sb.ToString();
        }

        protected virtual void ComputeOwnLayoutCss(StringBuilder sb)
        {
            sb.Append("display: grid; overflow: hidden; ");
            
            if (!double.IsNaN(Width))
                sb.Append($"width: {Width}px; ");

            if (!double.IsNaN(Height))
                sb.Append($"height: {Height}px; ");

            if (MinWidth > 0)
                sb.Append($"min-width: {MinWidth}px; ");

            if (MinHeight > 0)
                sb.Append($"min-height: {MinHeight}px; ");

            if (MaxWidth != double.PositiveInfinity)
                sb.Append($"max-width: {MaxWidth}px; ");

            if (MaxHeight != double.PositiveInfinity)
                sb.Append($"max-height: {MaxHeight}px; ");

            sb.Append($"margin: {ThicknessToCss(Margin)}; ");
            sb.Append($"padding: {ThicknessToCss(Padding)}; ");

            // If the element cannot effectively stretch due to size constraints (Width/MaxWidth),
            // CSS will align it at 'start' instead. To get the typical XAML behavior ('center'),
            // we explicitly set the CSS alignment to 'center' instead of 'stretch' in such cases.
            var canStretchH =
                HorizontalAlignment == Alignment.Stretch &&
                double.IsNaN(Width) && MaxWidth == double.PositiveInfinity;

            var canStretchV =
                VerticalAlignment == Alignment.Stretch &&
                double.IsNaN(Height) && MaxHeight == double.PositiveInfinity;

            // TODO: Remaining problem:
            //       If only MaxWidth is set (but no Width) and HorizontalAlignment == Stretch,
            //       the MaxWidth is not exhausted. Instead the element will size to content.

            // Here's an attempt at a workaround, but it breaks if MaxWidth exceeds available width
            //if (!canStretchH && MaxWidth != double.PositiveInfinity)
            //    sb.Append($"width: {MaxWidth}px; ");
            //if (!canStretchV && MaxHeight != double.PositiveInfinity)
            //    sb.Append($"height: {MaxHeight}px; ");

            sb.Append($"justify-self: {AlignmentToCss(HorizontalAlignment, canStretchH)}; ");
            sb.Append($"align-self: {AlignmentToCss(VerticalAlignment, canStretchV)}; ");
        }

        protected virtual void ComputeChildLayoutCss(StringBuilder sb, UIElement child)
        {
        }

        protected string AlignmentToCss(Alignment alignment, bool allowStretch)
        {
            switch (alignment)
            {
                case Alignment.Start: return "start";
                case Alignment.End: return "end";
                case Alignment.Center: return "center";
                case Alignment.Stretch: return allowStretch ? "stretch" : "center";
                default: throw new NotImplementedException();
            }
        }

        protected string ThicknessToCss(Thickness t) =>
            $"{t.Top}px {t.Right}px {t.Bottom}px {t.Left}px";
    }
}
