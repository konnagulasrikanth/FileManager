using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FileManager
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool booleanValue = (bool)value;
            if (parameter != null && parameter.ToString() == "!")
            {
                booleanValue = !booleanValue;
            }
            return booleanValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is Visibility visibility && visibility == Visibility.Visible);
        }
    }
}
