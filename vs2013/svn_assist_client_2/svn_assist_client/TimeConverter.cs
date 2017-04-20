using System;
using System.Globalization;
using System.Windows.Data;

namespace svn_assist_client
{
    public class TimeConverter :IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return TimeStyle.ConvertIntDateTime(double.Parse(value.ToString()));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return int.Parse(TimeStyle.ConvertDateTimeInt((DateTime)value).ToString());
        }
    }
}
