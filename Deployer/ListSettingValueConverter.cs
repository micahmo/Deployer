using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Deployer
{
    public class ListSettingValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (KnownValueConverters.Instance.FirstOrDefault(x => x.Key.IsInstanceOfType(value)).Value is { } valueConverter)
            {
                return valueConverter.Convert(value, targetType, parameter, culture);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (KnownValueConverters.Instance.FirstOrDefault(x => x.Key.IsInstanceOfType(value)).Value is { } valueConverter)
            {
                return valueConverter.ConvertBack(value, targetType, parameter, culture);
            }

            return value;
        }
    }
}
