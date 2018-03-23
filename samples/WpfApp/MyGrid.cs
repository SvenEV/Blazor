using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static System.Math;
using static WpfApp.MathEx;

namespace WpfApp
{
    class MyGrid : Panel
    {
        public List<RowDefinition> RowDefinitions { get; } = new List<RowDefinition>();

        public List<ColumnDefinition> ColumnDefinitions { get; } = new List<ColumnDefinition>();

        private GridSpan[] _rows;
        private GridSpan[] _cols;

        protected override Size MeasureOverride(Size availableSize)
        {
            var rows = RowDefinitions.Select(def => new GridSpan(def.Height, def.MinHeight, def.MaxHeight)).ToArray();
            var cols = ColumnDefinitions.Select(def => new GridSpan(def.Width, def.MinWidth, def.MaxWidth)).ToArray();
            var spans = new[] { cols, rows };

            var remainingSpace = availableSize;

            void ReduceRemainingSpace(int dim, double reduction) => SetCoord(ref remainingSpace, dim, Max(0, remainingSpace.Coord(dim) - reduction));

            // First, initialize span sizes with defined minimum sizes
            for (var dim = 0; dim <= 1; dim++)
            {
                foreach (var span in spans[dim])
                {
                    span.ComputedSize = span.DefinedMinSize; // can't get any smaller, if necessary the Grid is clipped instead
                    ReduceRemainingSpace(dim, span.DefinedMinSize);
                }
            }

            // Then, assign size of fixed-size spans
            for (var dim = 0; dim <= 1; dim++)
            {
                foreach (var span in spans[dim].Where(s => s.DefinedSize.IsAbsolute))
                {
                    var size = Min(Clamp(span.DefinedSize.Value, span.DefinedMinSize, span.DefinedMaxSize), remainingSpace.Coord(dim));
                    span.ComputedSize = size;
                    ReduceRemainingSpace(dim, size - span.DefinedMinSize);
                }
            }

            // Next, measure children (provide them with sizes of covered fixed-size spans [+ remainingWidth if auto/star spans are covered])
            foreach (UIElement child in Children)
            {
                var area = GetCoveredArea(child);
                var coveredCols = cols.Skip(area.X).Take(area.Width);
                var coveredRows = rows.Skip(area.Y).Take(area.Height);

                var coveredFixedWidth = coveredCols.Where(s => s.DefinedSize.IsAbsolute).Sum(s => s.ComputedSize);
                var coveredFixedHeight = coveredRows.Where(s => s.DefinedSize.IsAbsolute).Sum(s => s.ComputedSize);

                var providedWidth = coveredFixedWidth + (coveredCols.All(s => s.DefinedSize.IsAbsolute) ? 0 : remainingSpace.Width);
                var providedHeight = coveredFixedHeight + (coveredRows.All(s => s.DefinedSize.IsAbsolute) ? 0 : remainingSpace.Height);

                child.Measure(new Size(providedWidth, providedHeight));
                var childMeasuredSize = child.DesiredSize;

                // Child first fills up space of covered fixed-size spans. If that's not enough, enlarge covered auto-sized spans from left-to-right/top-to-bottom.
                var coveredSpans = new[] { coveredCols, coveredRows };
                var coveredFixedSpace = new[] { coveredFixedWidth, coveredFixedHeight };

                for (var dim = 0; dim <= 1; dim++)
                {
                    var spaceNotAssignableToFixedSizeSpans = Max(0, childMeasuredSize.Coord(dim) - coveredFixedSpace[dim]); // to be distributed among covered auto/star spans
                    var coveredAutoSpans = coveredSpans[dim].Where(s => s.DefinedSize.IsAuto);

                    // Child covers auto-sized spans, so one of these (or multiple together) may provide the remaining space to distribute.
                    // We try to assign the required space to the first auto-sized span. If that doesn't suffice (because of its defined max size),
                    // assign space to the next auto-sized span etc., until (A) space has been fully assigned, (B) we run out of total space for the grid,
                    // or (C) all covered spans have been filled up.
                    var remainingSpaceToDistribute = spaceNotAssignableToFixedSizeSpans;

                    foreach (var autoSpan in coveredAutoSpans)
                    {
                        // if all of the required space has been distributed (case A), the remaining covered spans are not needed and can be skipped
                        if (remainingSpaceToDistribute <= 0)
                            break;

                        var enlargement = Clamp(remainingSpaceToDistribute - autoSpan.ComputedSize, 0, autoSpan.DefinedMaxSize - autoSpan.ComputedSize);
                        enlargement = Min(enlargement, remainingSpace.Coord(dim)); // if overall remaining space doesn't suffice, grid content gets clipped
                        autoSpan.ComputedSize += enlargement;
                        ReduceRemainingSpace(dim, enlargement);
                        remainingSpaceToDistribute -= enlargement;
                    }
                }
            }

            // If any space is left, distribute it among star-sized spans
            for (var dim = 0; dim <= 1; dim++)
            {
                var starSpans = spans[dim].Where(s => s.DefinedSize.IsStar);
                var numStars = starSpans.Sum(span => span.DefinedSize.Value);
                var sizePerStar = remainingSpace.Coord(dim) / numStars;

                foreach (var span in starSpans)
                {
                    span.ComputedSize = Clamp(span.DefinedSize.Value * sizePerStar, span.DefinedMinSize, span.DefinedMaxSize);
                    ReduceRemainingSpace(dim, span.ComputedSize - span.DefinedMinSize);

                    // due to defined max size, span might not be able to be as large as its star size suggests
                    // so we have to recalculate the size of a star
                    // (alternative: keep the star size! In this case, Grid may not take up all the available space, though)
                    numStars -= span.DefinedSize.Value;
                    sizePerStar = remainingSpace.Coord(dim) / numStars;
                }
            }

            _rows = rows;
            _cols = cols;
            return new Size(availableSize.Width - remainingSpace.Width, availableSize.Height - remainingSpace.Height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (UIElement child in Children)
            {
                var area = GetCoveredArea(child);
                var offsetX = _cols.Take(area.X).Sum(s => s.ComputedSize);
                var offsetY = _rows.Take(area.Y).Sum(s => s.ComputedSize);
                var sizeX = _cols.Skip(area.X).Take(area.Width).Sum(s => s.ComputedSize);
                var sizeY = _rows.Skip(area.Y).Take(area.Height).Sum(s => s.ComputedSize);
                child.Arrange(new Rect(offsetX, offsetY, sizeX, sizeY));
            }

            return finalSize;
        }

        private static IntRect GetCoveredArea(UIElement e) => new IntRect(
            Grid.GetColumn(e), Grid.GetRow(e), Grid.GetColumnSpan(e), Grid.GetRowSpan(e));

        class GridSpan
        {
            public GridLength DefinedSize { get; }
            public double DefinedMinSize { get; }
            public double DefinedMaxSize { get; }
            public double ComputedSize { get; set; }

            public GridSpan(GridLength definedSize, double definedMinSize, double definedMaxSize)
            {
                DefinedSize = definedSize;
                DefinedMinSize = definedMinSize;
                DefinedMaxSize = definedMaxSize;
                ComputedSize = double.NaN;
            }
        }
    }

    struct IntRect
    {
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }

        public IntRect(int x, int y, int width, int height) : this()
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    public static class MathEx
    {
        public static double Clamp(double value, double min, double max) =>
            value < min ? min : (value > max ? max : value);

        public static double Coord(this Size size, int dimension) =>
            dimension == 0 ? size.Width :
            dimension == 1 ? size.Height :
            throw new ArgumentOutOfRangeException(nameof(dimension));

        public static void SetCoord(ref Size size, int dimension, double value)
        {
            switch (dimension)
            {
                case 0: size.Width = value; break;
                case 1: size.Height = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(dimension));
            }
        }
    }
}
