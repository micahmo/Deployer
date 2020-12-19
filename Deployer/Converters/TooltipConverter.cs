using System;
using System.Globalization;
using System.Windows.Data;

namespace Deployer
{
    /// <summary>
    /// Converts the string text associated with tooltips to remove empty space
    /// </summary>
    public class TooltipConverter : IValueConverter
    {
        #region IValueConverter members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString().Trim();
        }

        // Should not be converting from tooltip UI back to property
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
