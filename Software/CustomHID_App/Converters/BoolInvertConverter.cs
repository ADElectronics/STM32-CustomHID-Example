using System;
using System.Globalization;
using System.Windows.Data;

namespace CustomHID_App.Converters
{
    class BoolInvertConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((bool)value == true)
				return false;
			else
				return true;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((bool)value == true)
				return false;
			else
				return true;
		}
	}
}
