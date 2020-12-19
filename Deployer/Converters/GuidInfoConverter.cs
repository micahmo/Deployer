using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Deployer.Properties;

namespace Deployer
{
    public class GuidInfoConverter : IValueConverter
    {
        #region IValueConverter members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Configuration.Instance.ConfigurationItems.FirstOrDefault(c => c.GuidInfo.Equals(value as GuidInfo))?.Name ?? $"<{Resources.None}>";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as ConfigurationItem)?.GuidInfo;
        }

        #endregion
    }
}
