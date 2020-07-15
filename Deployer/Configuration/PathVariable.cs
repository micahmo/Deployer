#region Usings

using GalaSoft.MvvmLight;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

#endregion

namespace Deployer
{
    [Serializable]
    public class PathVariable : ObservableObject
    {
        public PathVariable()
        {
            PossibleValues.CollectionChanged += PossibleValues_CollectionChanged;
        }

        public string Name
        {
            get => _name;
            set => Set(nameof(Name), ref _name, value);
        }
        private string _name;

        /// <summary>
        /// Globally unique runtime ID (not persisted)
        /// </summary>
        [XmlIgnore]
        public Guid Guid { get; } = Guid.NewGuid();

        public ObservableCollection<PossiblePathVariable> PossibleValues { get; } = new ObservableCollection<PossiblePathVariable>();

        [XmlIgnore]
        public PossiblePathVariable SelectedValue
        {
            get => PossibleValues.FirstOrDefault(v => v.IsSelected);
            set => PossibleValues.Where(v => v == value).ToList().ForEach(v => v.IsSelected = true);
        }

        public PathVariable GenerateInitialValuesLists()
        {
            PossibleValues.Add(new PossiblePathVariable { Value = string.Empty, IsSelected = true });
            return this;
        }

        #region Event handlers

        private void PossibleValues_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is { })
            {
                foreach (PossiblePathVariable oldItem in e.OldItems.OfType<PossiblePathVariable>())
                {
                    oldItem.PropertyChanged -= PossiblePathVariable_PropertyChanged;
                }
            }

            if (e.NewItems is { })
            {
                foreach (PossiblePathVariable newItem in e.NewItems.OfType<PossiblePathVariable>())
                {
                    newItem.PropertyChanged += PossiblePathVariable_PropertyChanged;
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Remove && PossibleValues.Any(p => p.IsSelected) == false && PossibleValues.FirstOrDefault() is { } firstValue)
            {
                firstValue.IsSelected = true;
            }

            Configuration.Instance?.ConfigurationItems[Configuration.Instance.SelectedConfigurationIndex].SourceDirectories.ToList().ForEach(d => d.RaisePropertyChanged(nameof(d.Path)));
            Configuration.Instance?.ConfigurationItems[Configuration.Instance.SelectedConfigurationIndex].DestinationDirectories.ToList().ForEach(d => d.RaisePropertyChanged(nameof(d.Path)));
        }

        private void PossiblePathVariable_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is PossiblePathVariable changedValue)
            {
                switch (e.PropertyName)
                {
                    case nameof(PossiblePathVariable.IsSelected):
                    case nameof(PossiblePathVariable.Value):
                        if (changedValue.IsSelected)
                        {
                            PossibleValues.Where(p => p != changedValue).ToList().ForEach(p => p.IsSelected = false);
                            RaisePropertyChanged(nameof(SelectedValue));
                            
                            Configuration.Instance?.ConfigurationItems[Configuration.Instance.SelectedConfigurationIndex].SourceDirectories.ToList().ForEach(d => d.RaisePropertyChanged(nameof(d.Path)));
                            Configuration.Instance?.ConfigurationItems[Configuration.Instance.SelectedConfigurationIndex].DestinationDirectories.ToList().ForEach(d => d.RaisePropertyChanged(nameof(d.Path)));
                        }
                        break;
                }
            }
        }

        #endregion
    }

    public class PossiblePathVariable : ObservableObject
    {
        public string Value
        {
            get => _value;
            set => Set(nameof(Value), ref _value, value);
        }
        private string _value;

        public bool IsSelected
        {
            get => _isSelected;
            set => Set(nameof(IsSelected), ref _isSelected, value);
        }
        private bool _isSelected;
    }
}
