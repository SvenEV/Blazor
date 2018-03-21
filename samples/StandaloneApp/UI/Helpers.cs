using System.Collections.Generic;
using System.Linq;

namespace StandaloneApp.UI
{
    public static class Helpers
    {
        public static IReadOnlyList<RowDefinition> Rows(params string[] sizeStrings) =>
            sizeStrings.Select(s => new RowDefinition(GridLength.Parse(s))).ToList();

        public static IReadOnlyList<ColumnDefinition> Columns(params string[] sizeStrings) =>
            sizeStrings.Select(s => new ColumnDefinition(GridLength.Parse(s))).ToList();

        public static Thickness Thickness(double uniformSize) =>
            new Thickness(uniformSize, uniformSize, uniformSize, uniformSize);

        public static Thickness Thickness(double horizontalSize, double verticalSize) =>
            new Thickness(horizontalSize, verticalSize, horizontalSize, verticalSize);

        public static Thickness Thickness(double left, double top, double right, double bottom) =>
            new Thickness(left, top, right, bottom);

        public static Point Size(double width, double height) =>
            new Point(width, height);

        public static double Clamp(double value, double min, double max) =>
            value < min ? min : (value > max ? max : value);

        public static double OrIfNan(this double value, double fallbackValue) =>
            double.IsNaN(value) ? fallbackValue : value;
    }
}
