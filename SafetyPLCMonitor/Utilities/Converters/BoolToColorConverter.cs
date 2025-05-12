// SafetyPLCMonitor/Utilities/Converters/BoolToColorConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SafetyPLCMonitor.Utilities.Converters
{
    [ValueConversion(typeof(bool), typeof(Brush))]
    public class BoolToColorConverter : IValueConverter
    {
        public Brush TrueValue { get; set; } = new SolidColorBrush(Colors.Green);
        public Brush FalseValue { get; set; } = new SolidColorBrush(Colors.Gray);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool))
                return FalseValue;

            return (bool)value ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null; // 단방향 변환
        }
    }
}