#region Usings

using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation;
using System.Xml.Serialization;

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

            GeneralSettings = new SettingsGroup {Name = "General", Description = "General Settings"};
            GeneralSettings.Settings.Add(EnabledSetting);
            //GeneralSettings.Settings.Add(UpdateLiveSetting);

            CopySettings = new SettingsGroup {Name = "CopySettings", Description = "Copy Settings"};
            CopySettings.Settings.Add(LeftButNotRightSetting);
            CopySettings.Settings.Add(NewerOnLeftSetting);
            CopySettings.Settings.Add(NewerOnRightSetting);
            CopySettings.Settings.Add(ExclusionsList);

            LockedFileSettings = new SettingsGroup {Name = "LockedFileSettings", Description = "Locked File Settings"};
            LockedFileSettings.Settings.Add(LockedFileOptionSetting);

            ViewSettings = new SettingsGroup {Name = "ViewSettings", Description = "View Settings"};
            ViewSettings.Settings.Add(FileViewOptions);

            SettingsGroups = new List<SettingsGroup> {GeneralSettings, CopySettings, LockedFileSettings, ViewSettings};

            PropertyChanged += ConfigurationItem_PropertyChanged;
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

        private void ConfigurationItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(EnabledSetting):
                //case nameof(UpdateLiveSetting):
                    GeneralSettings.Settings.Clear();
                    GeneralSettings.Settings.Add(EnabledSetting);
                    //GeneralSettings.Settings.Add(UpdateLiveSetting);
                    break;
                case nameof(LeftButNotRightSetting):
                case nameof(NewerOnLeftSetting):
                case nameof(NewerOnRightSetting):
                case nameof(ExclusionsList):
                    CopySettings.Settings.Clear();
                    CopySettings.Settings.Add(LeftButNotRightSetting);
                    CopySettings.Settings.Add(NewerOnLeftSetting);
                    CopySettings.Settings.Add(NewerOnRightSetting);
                    CopySettings.Settings.Add(ExclusionsList);
                    break;
                case nameof(LockedFileOptionSetting):
                    LockedFileSettings.Settings.Clear();
                    LockedFileSettings.Settings.Add(LockedFileOptionSetting);
                    break;
                case nameof(FileViewOptions):
                    ViewSettings.Settings.Clear();
                    ViewSettings.Settings.Add(FileViewOptions);
                    break;
            }
        }

        #endregion

        #region Data members (public properties)

        public string Name { get; set; }

        public DirectoryCollection SourceDirectories { get; } = new DirectoryCollection();

        public DirectoryCollection DestinationDirectories { get; } = new DirectoryCollection();

        [XmlIgnore]
        public ObservableCollection<DirectoryPair> DirectoryPairs => EnabledSetting.Value ? _directoryPairs : new ObservableCollection<DirectoryPair>();
        private ObservableCollection<DirectoryPair> _directoryPairs = new ObservableCollection<DirectoryPair>();

        public Setting<bool> EnabledSetting
        {
            get => _enabledSetting;
            set => Set(nameof(EnabledSetting), ref _enabledSetting, value);
        }
        private Setting<bool> _enabledSetting = new Setting<bool>
        {
            Name = nameof(EnabledSetting), Description = "Enabled: ", SettingType = SettingType.Boolean, DefaultValue = true
        };

        public Setting<bool> UpdateLiveSetting
        {
            get => _updateLiveSetting;
            set => Set(nameof(UpdateLiveSetting), ref _updateLiveSetting, value);
        }
        private Setting<bool> _updateLiveSetting = new Setting<bool>
        {
            Name = nameof(UpdateLiveSetting), Description = "Update live:", SettingType = SettingType.Boolean, DefaultValue = false
        };

        public Setting<NonExistingFileOptions> LeftButNotRightSetting {
            get => _leftButNotRightSetting;
            set => Set(nameof(LeftButNotRightSetting), ref _leftButNotRightSetting, value);
        }
        private Setting<NonExistingFileOptions> _leftButNotRightSetting = new Setting<NonExistingFileOptions>
        {
            Name = nameof(LeftButNotRightSetting), Description = "If file exists on left but not on right:", SettingType = SettingType.List, DefaultValue = NonExistingFileOptions.Skip
        };

        public Setting<ExistingFileOptions> NewerOnLeftSetting
        {
            get => _newerOnLeftSetting;
            set => Set(nameof(NewerOnLeftSetting), ref _newerOnLeftSetting, value);
        }
        private Setting<ExistingFileOptions> _newerOnLeftSetting = new Setting<ExistingFileOptions>
        {
            Name = nameof(NewerOnLeftSetting), Description = "If file is newer on left:", SettingType = SettingType.List, DefaultValue = ExistingFileOptions.Replace
        };

        public Setting<ExistingFileOptions> NewerOnRightSetting
        {
            get => _newerOnRightSetting;
            set => Set(nameof(NewerOnRightSetting), ref _newerOnRightSetting, value);
        }
        private Setting<ExistingFileOptions> _newerOnRightSetting = new Setting<ExistingFileOptions>
        {
            Name = nameof(NewerOnRightSetting), Description = "If file is newer on right:", SettingType = SettingType.List, DefaultValue = ExistingFileOptions.Skip
        };

        public Setting<string> ExclusionsList
        {
            get => _exclusionsList;
            set => Set(nameof(ExclusionsList), ref _exclusionsList, value);
        }
        private Setting<string> _exclusionsList = new Setting<string>
        {
            Name = nameof(ExclusionsList), Description = "Exclusions list:", SettingType = SettingType.ExtendedString, DefaultValue = string.Empty,
            ExtendedDescription = $"One exclusion per line. Use * as wildcard.{Environment.NewLine}{Environment.NewLine}For example:{Environment.NewLine}*.exe{Environment.NewLine}*.config",
        };

        public Setting<LockedFileOptions> LockedFileOptionSetting
        {
            get => _lockedFileOptionsSetting;
            set => Set(nameof(LockedFileOptionSetting), ref _lockedFileOptionsSetting, value);
        }
        private Setting<LockedFileOptions> _lockedFileOptionsSetting = new Setting<LockedFileOptions>
        {
            Name = nameof(LockedFileOptionSetting), Description = "If destination file is locked:", SettingType = SettingType.List, DefaultValue = LockedFileOptions.StopLockingProcesses
        };

        public Setting<FileViewOptions> FileViewOptions
        {
            get => _fileViewOptions;
            set => Set(nameof(FileViewOptions), ref _fileViewOptions, value);
        }
        private Setting<FileViewOptions> _fileViewOptions = new Setting<FileViewOptions>
        {
            Name = nameof(FileViewOptions), Description = "File view options:", SettingType = SettingType.List, DefaultValue = Deployer.FileViewOptions.ViewAllFiles
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

                if (!string.IsNullOrEmpty(ExclusionsList.Value))
                {
                    string[] patterns = ExclusionsList.Value.Split(new[] {"\n", "\r\n", "\r"}, StringSplitOptions.RemoveEmptyEntries);
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

        public static string DefaultName = "New Configuration";

        #endregion
    }
}
