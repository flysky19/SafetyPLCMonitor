// SafetyPLCMonitor/Utilities/Converters/IOTypeVisibilityConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using SafetyPLCMonitor.Models;

namespace SafetyPLCMonitor.Utilities.Converters
{
    public enum IOVisibilityMode
    {
        Discrete,
        Register
    }

    [ValueConversion(typeof(IOType), typeof(Visibility))]
    public class IOTypeVisibilityConverter : IValueConverter
    {
        public IOVisibilityMode Mode { get; set; } = IOVisibilityMode.Discrete;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is IOType ioType))
                return Visibility.Collapsed;

            if (Mode == IOVisibilityMode.Discrete)
            {
                // 디스크릿 I/O인 경우 표시
                if (ioType == IOType.DiscreteInput || ioType == IOType.DiscreteOutput ||
                    ioType == IOType.VirtualInput || ioType == IOType.VirtualOutput)
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
            else // Register 모드
            {
                // 레지스터인 경우 표시
                if (ioType == IOType.InputRegister || ioType == IOType.HoldingRegister)
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null; // 단방향 변환
        }
    }
}