using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CustomHID_App.Converters
{
    class UInt16ToHexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            UInt16 intValue = 0;

            if (value == null || targetType != typeof(string))
                return DependencyProperty.UnsetValue;

            if (!UInt16.TryParse(value.ToString(), out intValue))
                return DependencyProperty.UnsetValue;

            return intValue.ToString("x4");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stringValue = value.ToString();
            UInt16 returnValue = 0;

            if (value == null || targetType != typeof(UInt16))
                return DependencyProperty.UnsetValue;

            try
            {
                returnValue = System.Convert.ToUInt16(stringValue, 16);
            }
            catch
            {
                return DependencyProperty.UnsetValue;
            }
            return returnValue;
        }
    }
}
