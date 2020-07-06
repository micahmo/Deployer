#region Usings

using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

#endregion

namespace Deployer
{
    public class Setting : ObservableObject
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public SettingType SettingType { get; set; }
    }

    [Serializable]
    [XmlInclude(typeof(Setting<bool>))]
    [XmlInclude(typeof(Setting<NonExistingFileOptions>))]
    [XmlInclude(typeof(Setting<ExistingFileOptions>))]
    [XmlInclude(typeof(Setting<LockedFileOptions>))]
    [XmlInclude(typeof(Setting<FileViewOptions>))]
    public class Setting<T> : Setting
    {
        public T Value
        {
            get => _value == null ? (T)(_value = DefaultValue) : (T)_value;
            set => Set(nameof(Value), ref _value, value);
        }
        private object _value;

        public T DefaultValue { get; set; } = default;

        [XmlIgnore]
        public List<T> PossibleValues
        {
            get
            {
                if (typeof(T).IsEnum)
                {
                    return Enum.GetValues(typeof(T)).OfType<T>().ToList();
                }
                else
                {
                    return new List<T>();
                }
            }
        }
    }

    public enum SettingType
    {
        Boolean,
        List
    }
}
