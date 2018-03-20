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
        public GridLength Size { get; set; }

        public RowDefinition(GridLength size) => Size = size;
    }

    public class ColumnDefinition
    {
        public GridLength Size { get; set; }

        public ColumnDefinition(GridLength size) => Size = size;
    }

    public struct GridLength
    {
        public double Value { get; }

        public GridUnitType UnitType { get; }

        public GridLength(double value, GridUnitType unitType) : this()
        {
            Value = value;
            UnitType = unitType;
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
