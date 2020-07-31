#region Usings

using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

#endregion

namespace Deployer
{
    #region Setting class

    public class Setting : ObservableObject
    {
        [XmlIgnore]
        public string Name { get; set; }

        [XmlIgnore]
        public string Description { get; set; }

        [XmlIgnore]
        public string ExtendedDescription { get; set; }

        [XmlIgnore]
        public SettingType SettingType { get; set; }

        [XmlIgnore]
        public List<DependentSettingCollection> DependentSettings { get; } = new List<DependentSettingCollection>();

        [XmlIgnore]
        public bool IsOptional { get; set; }

        /// <summary>
        /// This value is only relevant if <see cref="IsOptional"/> is <see langword="true"/>.
        /// </summary>
        public bool OptionSelected
        {
            get => InternalOptionSelected ?? default;
            set => Set(nameof(OptionSelected), ref InternalOptionSelected, value);
        }
        internal bool? InternalOptionSelected;
    }

    #endregion

    #region Setting<T> class

    [Serializable]
    public class Setting<T> : Setting
    {
        public void Apply(Setting<T> settingToApply)
        {
            Value = settingToApply.Value;

            if (settingToApply.InternalOptionSelected.HasValue)
            {
                OptionSelected = settingToApply.OptionSelected;
            }
        }

        public T Value
        {
            get => _value == null ? (T)(_value = DefaultValue) : (T)_value;
            set => Set(nameof(Value), ref _value, value);
        }
        private object _value;

        [XmlIgnore]
        public T DefaultValue { get; set; } = default;

        [XmlIgnore]
        public List<T> PossibleValues
        {
            get
            {
                if (PossibleValuesDelegate is { })
                {
                    return PossibleValuesDelegate?.Invoke()?.ToList();
                }
                else if (typeof(T).IsEnum)
                {
                    return Enum.GetValues(typeof(T)).OfType<T>().ToList();
                }
                else
                {
                    return new List<T>();
                }
            }
        }

        /// <summary>
        /// Defines a method which can generate the <see cref="PossibleValues"/> of the setting.
        /// Leave unassigned for non-list-type settings, Enum settings, or any other setting that does not need custom value list generation
        /// </summary>
        [XmlIgnore]
        public Func<IEnumerable<T>> PossibleValuesDelegate { get; set; }
    }

    #endregion

    #region DependentSettingCollection class

    public class DependentSettingCollection : ObservableObject
    {
        public DependentSettingCollection(Func<bool> conditionToShow, Setting parentSetting, params Setting[] dependentSettings)
        {
            ConditionToShow = conditionToShow;

            parentSetting.PropertyChanged += (sender, args) => { RaisePropertyChanged(nameof(Show)); };

            foreach (Setting dependentSetting in dependentSettings)
            {
                Settings.Add(dependentSetting);
            }
        }

        public IList<Setting> Settings { get; } = new List<Setting>();

        public bool Show => ConditionToShow?.Invoke() == true;

        public Func<bool> ConditionToShow { get; set; }
    }

    #endregion

    #region SettingType enum

    public enum SettingType
    {
        Boolean,
        List,
        ExtendedString
    }

    #endregion
}
