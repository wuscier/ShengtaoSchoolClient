using System;
using System.Globalization;
using System.Windows.Data;

namespace St.Common
{
    public class OnlineStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool status = value != null && (bool) value;
            return status ? "在线" : "离线";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                RunEnv runEnv = (RunEnv)Enum.Parse(typeof(RunEnv), value.ToString());
                return runEnv;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}