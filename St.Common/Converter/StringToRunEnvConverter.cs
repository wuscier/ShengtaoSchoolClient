using System;
using System.Globalization;
using System.Windows.Data;

namespace St.Common
{
    public class RunEnvToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string runDevDescription = EnumHelper.GetDescription(typeof(RunEnv), value);
                return runDevDescription;
            }
            catch (Exception)
            {
                return null;
            }
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