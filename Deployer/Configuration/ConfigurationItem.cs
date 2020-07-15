#region Usings

using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Management.Automation;
using System.Xml.Serialization;
using Deployer.Properties;

#endregion

namespace Deployer
{
    /// <summary>
    /// Defines a single Configuration item
    /// </summary>
    [Serializable]
    public class ConfigurationItem : ObservableObject
    {
        #region Constructor

        public ConfigurationItem()
        {
            SourceDirectories.CollectionChanged += Directories_CollectionChanged;
            DestinationDirectories.CollectionChanged += Directories_CollectionChanged;

            GeneralSettings = new SettingsGroup {Name = nameof(GeneralSettings), Description = Resources.GeneralSettingsDescription};
            GeneralSettings.Settings.Add(EnabledSetting);
            GeneralSettings.Settings.Add(IncludeDirectoriesSetting);
            //GeneralSettings.Settings.Add(UpdateLiveSetting);

            CopySettings = new SettingsGroup {Name = nameof(CopySettings), Description = Resources.CopySettingsDescription};
            CopySettings.Settings.Add(LeftButNotRightSetting);
            CopySettings.Settings.Add(NewerOnLeftSetting);
            CopySettings.Settings.Add(NewerOnRightSetting);
            CopySettings.Settings.Add(ExclusionsListSetting);

            LockedFileSettings = new SettingsGroup {Name = nameof(LockedFileSettings), Description = Resources.LockedFileSettingsDescription};
            LockedFileSettings.Settings.Add(LockedFileOptionSetting);
            LockedFileOptionSetting.DependentSettings.Add(new DependentSettingCollection(() => LockedFileOptionSetting.Value == LockedFileOptions.StopLockingProcesses, LockedFileOptionSetting, KilledProcessesSetting));

            ViewSettings = new SettingsGroup {Name = nameof(ViewSettings), Description = Resources.ViewSettingsDescription};
            ViewSettings.Settings.Add(FileViewOptionsSetting);

            SettingsGroups = new List<SettingsGroup> {GeneralSettings, CopySettings, LockedFileSettings, ViewSettings};
        }

        #endregion

        #region Public methods

        public ConfigurationItem GenerateInitialDirectoryLists()
        {
            SourceDirectories.Add(new DirectoryItem {Path = string.Empty});
            DestinationDirectories.Add(new DirectoryItem {Path = string.Empty});
            return this;
        }

        #endregion

        #region Event handlers

        private void Directories_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IEnumerable<DirectoryPair> oldDirectoryPairs = _directoryPairs.Select(d => d).ToList();
            _directoryPairs.Clear();

            foreach (DirectoryItem sourceDirectory in SourceDirectories)
            {
                foreach (DirectoryItem destinationDirectory in DestinationDirectories)
                {
                    if (oldDirectoryPairs.FirstOrDefault(d => Utilities.FileSystem.NormalizePath(d.Left.Path) == Utilities.FileSystem.NormalizePath(sourceDirectory.Path)
                                                           && Utilities.FileSystem.NormalizePath(d.Right.Path) == Utilities.FileSystem.NormalizePath(destinationDirectory.Path))
                        is { } existingDirectoryPair)
                    {
                        _directoryPairs.Add(existingDirectoryPair);
                    }
                    else
                    {
                        _directoryPairs.Add(new DirectoryPair(sourceDirectory, destinationDirectory, this));
                    }
                }
            }
        }

        #endregion

        #region Data members (public properties)

        public string Name { get; set; }

        /// <summary>
        /// Globally unique runtime ID (not persisted)
        /// </summary>
        [XmlIgnore]
        public Guid Guid { get; } = Guid.NewGuid();

        public DirectoryCollection SourceDirectories { get; } = new DirectoryCollection();

        public DirectoryCollection DestinationDirectories { get; } = new DirectoryCollection();

        [XmlIgnore]
        public ObservableCollection<DirectoryPair> DirectoryPairs => EnabledSetting.Value ? _directoryPairs : new ObservableCollection<DirectoryPair>();
        private ObservableCollection<DirectoryPair> _directoryPairs = new ObservableCollection<DirectoryPair>();

        public Setting<bool> EnabledSetting
        {
            get => _enabledSetting;
            set => _enabledSetting.Apply(value);
        }
        private Setting<bool> _enabledSetting = new Setting<bool>
        {
            Name = nameof(EnabledSetting), Description = Resources.EnabledSettingDescription,
            SettingType = SettingType.Boolean, DefaultValue = true
        };

        public Setting<bool> IncludeDirectoriesSetting
        {
            get => _includeDirectoriesSetting;
            set => _includeDirectoriesSetting.Apply(value);
        }
        private Setting<bool> _includeDirectoriesSetting = new Setting<bool>
        {
            Name = nameof(IncludeDirectoriesSetting), 
            Description = Resources.IncludeDirectoriesSettingDescription,
            ExtendedDescription = Resources.IncludeDirectoriesSettingExtendedDescription,
            SettingType = SettingType.Boolean, DefaultValue = false
        };

        public Setting<bool> UpdateLiveSetting
        {
            get => _updateLiveSetting;
            set => _updateLiveSetting.Apply(value);
        }
        private Setting<bool> _updateLiveSetting = new Setting<bool>
        {
            Name = nameof(UpdateLiveSetting),
            Description = Resources.UpdateLiveSettingDescription,
            SettingType = SettingType.Boolean, DefaultValue = false
        };

        public Setting<NonExistingFileOptions> LeftButNotRightSetting
        {
            get => _leftButNotRightSetting;
            set => _leftButNotRightSetting.Apply(value);
        }
        private Setting<NonExistingFileOptions> _leftButNotRightSetting = new Setting<NonExistingFileOptions>
        {
            Name = nameof(LeftButNotRightSetting), 
            Description = Resources.LeftButNotRightSettingDescription,
            SettingType = SettingType.List, DefaultValue = NonExistingFileOptions.Skip
        };

        public Setting<ExistingFileOptions> NewerOnLeftSetting
        {
            get => _newerOnLeftSetting;
            set => _newerOnLeftSetting.Apply(value);
        }
        private Setting<ExistingFileOptions> _newerOnLeftSetting = new Setting<ExistingFileOptions>
        {
            Name = nameof(NewerOnLeftSetting),
            Description = Resources.NewerOnLeftSettingDescription,
            SettingType = SettingType.List, DefaultValue = ExistingFileOptions.Replace
        };

        public Setting<ExistingFileOptions> NewerOnRightSetting
        {
            get => _newerOnRightSetting;
            set => _newerOnRightSetting.Apply(value);
        }
        private Setting<ExistingFileOptions> _newerOnRightSetting = new Setting<ExistingFileOptions>
        {
            Name = nameof(NewerOnRightSetting),
            Description = Resources.NewerOnRightSettingDescription,
            SettingType = SettingType.List, DefaultValue = ExistingFileOptions.Skip
        };

        public Setting<string> ExclusionsListSetting
        {
            get => _exclusionsListSetting;
            set => _exclusionsListSetting.Apply(value);
        }
        private Setting<string> _exclusionsListSetting = new Setting<string>
        {
            Name = nameof(ExclusionsListSetting), SettingType = SettingType.ExtendedString, DefaultValue = string.Empty,
            Description = Resources.ExclusionsListSettingDescription,
            ExtendedDescription = Resources.ExclusionsListSettingExtendedDescription
        };

        public Setting<LockedFileOptions> LockedFileOptionSetting
        {
            get => _lockedFileOptionsSetting;
            set => _lockedFileOptionsSetting.Apply(value);
        }
        private Setting<LockedFileOptions> _lockedFileOptionsSetting = new Setting<LockedFileOptions>
        {
            Name = nameof(LockedFileOptionSetting), SettingType = SettingType.List, DefaultValue = LockedFileOptions.StopLockingProcesses,
            Description = Resources.LockedFileOptionSettingDescription,
            ExtendedDescription = Resources.LockedFileOptionSettingExtendedDescription
        };

        public Setting<bool> KilledProcessesSetting
        {
            get => _killedProcessesSetting;
            set => _killedProcessesSetting.Apply(value);
        }
        private Setting<bool> _killedProcessesSetting = new Setting<bool>
        {
            Name = nameof(KilledProcessesSetting),
            Description = Resources.KilledProcessesSettingDescription,
            SettingType = SettingType.Boolean, DefaultValue = true
        };

        public Setting<FileViewOptions> FileViewOptionsSetting
        {
            get => _fileViewOptionsSetting;
            set => _fileViewOptionsSetting.Apply(value);
        }
        private Setting<FileViewOptions> _fileViewOptionsSetting = new Setting<FileViewOptions>
        {
            Name = nameof(FileViewOptionsSetting),
            Description = Resources.FileViewOptionsSettingDescription,
            SettingType = SettingType.List, DefaultValue = Deployer.FileViewOptions.ViewAllFiles
        };

        [XmlIgnore]
        public IEnumerable<SettingsGroup> SettingsGroups { get; }

        [XmlIgnore]
        public SettingsGroup GeneralSettings { get; }

        [XmlIgnore]
        public SettingsGroup CopySettings { get; }

        [XmlIgnore]
        public SettingsGroup LockedFileSettings { get; }

        [XmlIgnore]
        public SettingsGroup ViewSettings { get; }

        [XmlIgnore]
        public List<WildcardPattern> ExclusionListPatterns 
        {
            get
            {
                List<WildcardPattern> exclusionListPatterns = new List<WildcardPattern>();

                if (!string.IsNullOrEmpty(ExclusionsListSetting.Value))
                {
                    string[] patterns = ExclusionsListSetting.Value.Split(new[] {"\n", "\r\n", "\r"}, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string pattern in patterns)
                    {
                        exclusionListPatterns.Add(new WildcardPattern(pattern, WildcardOptions.Compiled | WildcardOptions.CultureInvariant | WildcardOptions.IgnoreCase));
                    }
                }

                return exclusionListPatterns;
            }
        }

        #endregion

        #region Public static fields

        public static string DefaultName = Resources.NewConfigurationName;

        #endregion
    }
}
