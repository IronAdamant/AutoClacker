using System;
using System.Windows.Data;

namespace AutoClacker.Converters
{
    public class EnumToCommandConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (parameter == null)
                return Binding.DoNothing;
            return parameter;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (parameter == null)
                return Binding.DoNothing;
            return Enum.Parse(targetType, parameter.ToString());
        }
    }
}