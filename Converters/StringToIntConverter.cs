using System;
using System.Windows.Data;

namespace AutoClacker.Converters
{
    public class StringToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value?.ToString() ?? "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string input = value as string;
            if (string.IsNullOrWhiteSpace(input))
                return 0; // Return 0 for empty input

            if (int.TryParse(input, out int result))
                return result;

            return 0; // Return 0 for invalid input
        }
    }
}