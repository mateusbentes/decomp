using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace DecompilerGUI.ViewModels
{
    public class CultureInfoToStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is CultureInfo cultureInfo)
            {
                return cultureInfo.Name;
            }
            return value?.ToString() ?? string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string cultureName)
            {
                return new CultureInfo(cultureName);
            }
            return CultureInfo.CurrentCulture;
        }
    }
}
