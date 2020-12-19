using System;
using System.Globalization;
using System.Windows.Data;
using Humanizer;

namespace Deployer
{
    public class EnumDescriptionConverter : IValueConverter
    {
        #region IValueConverter members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string result = value?.ToString();

            if (value is Enum member)
            {
                result = member.Humanize();
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
