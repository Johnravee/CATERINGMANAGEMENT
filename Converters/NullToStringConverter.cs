using System;
using System.Globalization;
using System.Windows.Data;

namespace CATERINGMANAGEMENT.Converters
{
    public class NullToStringConverter : IValueConverter
    {
        public string FallbackText { get; set; } = "N/A";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return FallbackText;

            return value?.ToString() ?? FallbackText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

