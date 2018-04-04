using System;
using System.Collections.Generic;
using System.Linq;

namespace StandaloneApp.UI
{
    /// <summary>
    /// Constants and static methods to simplify writing Xamzor views.
    /// </summary>
    public static class Helpers
    {
        public const Orientation Horizontal = Orientation.Horizontal;
        public const Orientation Vertical = Orientation.Vertical;
        public const Alignment Left = Alignment.Start;
        public const Alignment Top = Alignment.Start;
        public const Alignment Right = Alignment.End;
        public const Alignment Bottom = Alignment.End;
        public const Alignment Center = Alignment.Center;
        public const Alignment Stretch = Alignment.Stretch;
        public const ImageStretch None = UI.ImageStretch.None;
        public const ImageStretch Fill = UI.ImageStretch.Fill;
        public const ImageStretch Uniform = UI.ImageStretch.Uniform;
        public const ImageStretch UniformToFill = UI.ImageStretch.UniformToFill;

        public static IReadOnlyList<RowDefinition> Rows(params string[] sizeStrings) =>
            sizeStrings.Select(s => new RowDefinition(GridLength.Parse(s))).ToList();

        public static IReadOnlyList<ColumnDefinition> Columns(params string[] sizeStrings) =>
            sizeStrings.Select(s => new ColumnDefinition(GridLength.Parse(s))).ToList();

        public static double Clamp(double value, double min, double max) =>
            value < min ? min : (value > max ? max : value);

        public static double OrIfNan(this double value, double fallbackValue) =>
            double.IsNaN(value) ? fallbackValue : value;

        public static bool EqualsApprox(double a, double b, double tolerance = .001) =>
            Math.Abs(a - b) <= tolerance;
    }
}
