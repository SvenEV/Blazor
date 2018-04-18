using Microsoft.AspNetCore.Blazor;
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

        public void AttachToView(ApplicationView view) => View = view;

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
            ComputeLayoutCss(sb);
            LayoutCss = sb.ToString();
        }

        protected virtual void ComputeLayoutCss(StringBuilder sb)
        {
            sb.Append("display: grid; overflow: hidden;");
            sb.Append("justify-self: " + AlignmentToCss(HorizontalAlignment));
            sb.Append("align-self: " + AlignmentToCss(VerticalAlignment));
            sb.Append($"grid-area: {Row + 1} / {Column + 1} / span {RowSpan} / span {ColumnSpan};");

            string AlignmentToCss(Alignment alignment)
            {
                switch (alignment)
                {
                    case Alignment.Start: return "start";
                    case Alignment.End: return "end";
                    case Alignment.Center: return "center";
                    case Alignment.Stretch: return "stretch";
                    default: throw new NotImplementedException();
                }
            }
        }
    }
}
