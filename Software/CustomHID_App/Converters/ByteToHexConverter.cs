using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CustomHID_App.Converters
{
    class ByteToHexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            byte byteValue = 0; 
            
            if (value == null || targetType != typeof(string)) 
                return DependencyProperty.UnsetValue;
            
            if (!byte.TryParse(value.ToString(), out byteValue)) 
                return DependencyProperty.UnsetValue;

            return byteValue.ToString("x2");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stringValue = value.ToString();
            byte returnValue = 0;

            if (value == null || targetType != typeof(byte)) 
                return DependencyProperty.UnsetValue;

            try 
            { 
                returnValue = System.Convert.ToByte(stringValue, 16);
            }
            catch 
            { 
                return DependencyProperty.UnsetValue; 
            }
            return returnValue;
        }
    }
}
