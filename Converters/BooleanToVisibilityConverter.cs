using System;
using System.Windows;
using System.Windows.Data;

namespace AutoClacker.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isVisible = (bool)value;
            if (parameter != null && parameter.ToString() == "Inverse")
                isVisible = !isVisible;
            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;
            bool isVisible = visibility == Visibility.Visible;
            if (parameter != null && parameter.ToString() == "Inverse")
                isVisible = !isVisible;
            return isVisible;
        }
    }
}