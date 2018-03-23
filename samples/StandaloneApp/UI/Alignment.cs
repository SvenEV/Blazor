using System;

namespace StandaloneApp.UI
{
    public enum Alignment
    {
        Stretch, Start, Center, End
    }

    public enum Orientation
    {
        Horizontal, Vertical
    }

    public class RowDefinition
    {
        public GridLength Height { get; set; }

        public double MinHeight { get; set; } = 0;

        public double MaxHeight { get; set; } = double.PositiveInfinity;

        public RowDefinition(GridLength size) => Height = size;
    }

    public class ColumnDefinition
    {
        public GridLength Width { get; set; }

        public double MinWidth { get; set; } = 0;

        public double MaxWidth { get; set; } = double.PositiveInfinity;

        public ColumnDefinition(GridLength size) => Width = size;
    }

    public struct GridLength
    {
        public double Value { get; }

        public GridUnitType GridUnitType { get; }

        public bool IsAbsolute => GridUnitType == GridUnitType.Absolute;

        public bool IsAuto => GridUnitType == GridUnitType.Auto;

        public bool IsStar => GridUnitType == GridUnitType.Star;

        public GridLength(double value, GridUnitType unitType) : this()
        {
            Value = value;
            GridUnitType = unitType;
        }

        public static GridLength Parse(string s)
        {
            if (s == "*")
                return new GridLength(1, GridUnitType.Star);

            if (s.Equals("auto", StringComparison.OrdinalIgnoreCase))
                return new GridLength(1, GridUnitType.Auto);

            if (double.TryParse(s, out var absSize))
                return new GridLength(absSize, GridUnitType.Absolute);

            if (s.EndsWith("*") && double.TryParse(s.Substring(0, s.Length - 1), out var starSize))
                return new GridLength(starSize, GridUnitType.Star);

            throw new FormatException($"'{s}' is not a valid format for '{nameof(GridLength)}'");
        }
    }

    public enum GridUnitType
    {
        Absolute, Star, Auto
    }
}
