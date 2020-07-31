using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Deployer.Properties;

namespace Deployer
{
    [Serializable]
    public class GuidInfo
    {
        #region Public static properties/methods

        public static GuidInfo Empty { get; } = new GuidInfo(Guid.Empty);

        public static GuidInfo NewGuid() => new GuidInfo(Guid.NewGuid());

        #endregion
         
        #region Constructor

        public GuidInfo()
        {
            Guid = new Guid();
        }

        private GuidInfo(Guid guid)
        {
            Guid = guid;
        }

        #endregion

        #region Public properties

        public Guid Guid { get; set; }

        #endregion

        #region Public methods

        public ConfigurationItem GetConfigurationItem()
        {
            return Configuration.Instance.ConfigurationItems.FirstOrDefault(c => c.GuidInfo.Equals(this));
        }

        #endregion

        #region Equality overrides

        public override bool Equals(object obj)
        {
            return Guid == (obj as GuidInfo)?.Guid;
        }

        protected bool Equals(GuidInfo other)
        {
            return Guid == other.Guid;
        }

        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            // This warning is generated because Guid has a public setter; however, it must in order to be deserialized.
            return Guid.GetHashCode();
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return Guid.ToString();
        }

        #endregion
    }

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
