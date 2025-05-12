// SafetyPLCMonitor/Utilities/Converters/BoolToVisibilityConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SafetyPLCMonitor.Utilities.Converters
{
    /// <summary>
    /// Boolean 값을 Visibility로 변환하는 컨버터.
    /// true -> Visible, false -> Collapsed
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// true 값에 대한 Visibility 값을 지정합니다. 기본값은 Visible입니다.
        /// </summary>
        public Visibility TrueValue { get; set; } = Visibility.Visible;

        /// <summary>
        /// false 값에 대한 Visibility 값을 지정합니다. 기본값은 Collapsed입니다.
        /// </summary>
        public Visibility FalseValue { get; set; } = Visibility.Collapsed;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool))
                return FalseValue;

            return (bool)value ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Visibility))
                return false;

            return (Visibility)value == TrueValue;
        }
    }
}