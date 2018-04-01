using System.Collections.Generic;
using System.Linq;

namespace StandaloneApp.UI
{
    /// <summary>
    /// Constants and static methods to simplify writing Xamzor views.
    /// </summary>
    public static class Helpers
    {
        public static readonly Orientation Horizontal = Orientation.Horizontal;
        public static readonly Orientation Vertical = Orientation.Vertical;
        public static readonly Alignment Left = Alignment.Start;
        public static readonly Alignment Top = Alignment.Start;
        public static readonly Alignment Right = Alignment.End;
        public static readonly Alignment Bottom = Alignment.End;
        public static readonly Alignment Center = Alignment.Center;
        public static readonly Alignment Stretch = Alignment.Stretch;

        public static IReadOnlyList<RowDefinition> Rows(params string[] sizeStrings) =>
            sizeStrings.Select(s => new RowDefinition(GridLength.Parse(s))).ToList();

        public static IReadOnlyList<ColumnDefinition> Columns(params string[] sizeStrings) =>
            sizeStrings.Select(s => new ColumnDefinition(GridLength.Parse(s))).ToList();

        public static double Clamp(double value, double min, double max) =>
            value < min ? min : (value > max ? max : value);

        public static double OrIfNan(this double value, double fallbackValue) =>
            double.IsNaN(value) ? fallbackValue : value;
    }
}
