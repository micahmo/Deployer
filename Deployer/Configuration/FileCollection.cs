﻿#region Usings

using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

#endregion

namespace Deployer
{
    public class FileCollection : ObservableObject, IDisposable
    {
        #region Constructor

        public FileCollection(DirectoryItem directory, DirectoryItem otherDirectory, ConfigurationItem configurationItem)
        {
            _directory = directory;
            _otherDirectory = otherDirectory;

            _configurationItem = configurationItem;
            _configurationItem.PropertyChanged += _configurationItem_PropertyChanged;
            _configurationItem.ViewSettings.Settings.ToList().ForEach(s => s.PropertyChanged += ConfigurationItemSetting_PropertyChanged);
            _configurationItem.CopySettings.Settings.ToList().ForEach(s => s.PropertyChanged += ConfigurationItemSetting_PropertyChanged);

            directory.PropertyChanged += Directory_PropertyChanged;
            otherDirectory.PropertyChanged += Directory_PropertyChanged;

            //Utilities.FileSystem.RegisterFileSystemWatcherEvents(directory.FileSystemWatcher, FileSystemWatcher_Changed);
            //Utilities.FileSystem.RegisterFileSystemWatcherEvents(otherDirectory.FileSystemWatcher, FileSystemWatcher_Changed);

            //_timer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(1)};
            //_timer.Tick += Timer_Tick;
            //_timer.Start();
        }

        //private void Timer_Tick(object sender, EventArgs e)
        //{
        //    if (Dirty && _configurationItem.DirectoryPairs.Any(d => d.LeftFileCollection == this || d.RightFileCollection == this) 
        //              && _configurationItem?.UpdateLiveSetting.Value == true)
        //    {
        //        UpdateFileCollection();
        //        Dirty = false;
        //    }
        //}

        #endregion

        #region Private methods

        private void UpdateFileCollection()
        {
            using (new WaitCursor(DispatcherPriority.ApplicationIdle, restoreCursorToNull: true))
            {
                if (Configuration.Instance is { } && _configurationItem.EnabledSetting.Value)
                {
                    Task.Run(() =>
                    {
                        lock (_fileUpdateLock)
                        {
                            _threadCancellationTokenSource?.Cancel();
                            _files.Clear();
                            _files.AddRange(GenerateFileCollection());
                            _threadCancellationTokenSource = IconHelper.GetIcons(this);

                            Application.Current?.Dispatcher.Invoke(() => { RaisePropertyChanged(nameof(CountLabel)); });
                        }

                        Application.Current?.Dispatcher.Invoke(() => { RaisePropertyChanged(nameof(Files)); });
                    });
                }
            }
        }

        private readonly object _fileUpdateLock = new object();

        public List<FileItem> GenerateFileCollection(FileViewOptions? fileViewOptions = null)
        {
            bool filter = (fileViewOptions ?? _configurationItem.FileViewOptions.Value) == FileViewOptions.ViewPendingFiles;

            // Get these files
            List<InternalFileItem> directoryFileList = new List<InternalFileItem>(_directory.GetFileInfos().Select(f => new InternalFileItem(f, _directory.Path)
            {
                Other = false,
                Excluded = _configurationItem.ExclusionListPatterns.Any(p => p.IsMatch(f.Name))
            }));

            if (_configurationItem.IncludeDirectoriesSetting.Value)
            {
                // Get these directories
                directoryFileList.AddRange(new List<InternalFileItem>(_directory.GetDirectoryInfos().Select(f => new InternalFileItem(f, _directory.Path)
                {
                    Other = false,
                    Excluded = _configurationItem.ExclusionListPatterns.Any(p => p.IsMatch(f.Name))
                })));
            }

            // Get other files
            List<InternalFileItem> otherDirectoryFileList = new List<InternalFileItem>(_otherDirectory.GetFileInfos().Select(f => new InternalFileItem(f, _otherDirectory.Path)
            {
                Other = true,
                Excluded = _configurationItem.ExclusionListPatterns.Any(p => p.IsMatch(f.Name))
            }));

            if (_configurationItem.IncludeDirectoriesSetting.Value)
            {
                // Get other directories
                otherDirectoryFileList.AddRange(new List<InternalFileItem>(_otherDirectory.GetDirectoryInfos().Select(f => new InternalFileItem(f, _otherDirectory.Path)
                {
                    Other = true,
                    Excluded = _configurationItem.ExclusionListPatterns.Any(p => p.IsMatch(f.Name))
                })));
            }

            FileItemEqualityComparer fileItemEqualityComparer = new FileItemEqualityComparer();
            FileItemComparer fileItemComparer = new FileItemComparer();
            foreach (InternalFileItem file in directoryFileList)
            {
                if (otherDirectoryFileList.FirstOrDefault(f => fileItemEqualityComparer.Equals(file, f)) is { } otherFile)
                {
                    file.Overwrite = fileItemComparer.Compare(file, otherFile) > 0;
                    file.GetOverwritten = fileItemComparer.Compare(otherFile, file) > 0;
                    file.HasOther = true;
                    otherDirectoryFileList.Remove(otherFile);
                }
            }

            directoryFileList.AddRange(otherDirectoryFileList);

            // Filter if needed
            if (filter)
            {
                // Always remove files that exist in both places but are neither overwriting nor being overwritten
                directoryFileList.RemoveAll(f => f.HasOther && !(f.Overwrite || f.GetOverwritten));

                // Always remove files that are being excluded
                directoryFileList.RemoveAll(f => f.Excluded);

                if (IsLeft)
                {
                    if (_configurationItem.NewerOnLeftSetting.Value == ExistingFileOptions.Skip)
                    {
                        directoryFileList.RemoveAll(f => f.Overwrite);
                    }

                    if (_configurationItem.NewerOnRightSetting.Value == ExistingFileOptions.Skip)
                    {
                        directoryFileList.RemoveAll(f => f.GetOverwritten);
                    }

                    if (_configurationItem.LeftButNotRightSetting.Value == NonExistingFileOptions.Skip)
                    {
                        directoryFileList.RemoveAll(f => !f.HasOther);
                    }

                    // There is no option to copy from Right to Left, so in filter mode, we never show Other files on the left side
                    directoryFileList.RemoveAll(f => f.Other);
                }

                if (IsRight)
                {
                    if (_configurationItem.NewerOnRightSetting.Value == ExistingFileOptions.Skip)
                    {
                        directoryFileList.RemoveAll(f => f.Overwrite);
                    }

                    if (_configurationItem.NewerOnLeftSetting.Value == ExistingFileOptions.Skip)
                    {
                        directoryFileList.RemoveAll(f => f.GetOverwritten);
                    }

                    if (_configurationItem.LeftButNotRightSetting.Value == NonExistingFileOptions.Skip)
                    {
                        directoryFileList.RemoveAll(f => f.Other);
                    }

                    // There is no option to copy from Right to Left, so in filter mode, we never show HasOther files on the right side
                    directoryFileList.RemoveAll(f => !(f.HasOther || f.Other));
                }
            }

            return directoryFileList.OfType<FileItem>().ToList();
        }

        #endregion

        #region Event handlers

        //private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        //{
        //    Dirty = true;
        //}

        private void Directory_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is DirectoryItem directoryItem)
            {
                switch (e.PropertyName)
                {
                    case nameof(directoryItem.Path):
                        if (directoryItem == _directory)
                        {
                            // Always update the file collection if our path changes
                            UpdateFileCollection();
                        }
                        else if (_files.Count == 0)
                        {
                            // Otherwise, this is the other directory. But our file list is empty. So update it.
                            UpdateFileCollection();
                        }

                        break;
                    //case nameof(DirectoryItem.FileSystemWatcher):
                    //    Utilities.FileSystem.RegisterFileSystemWatcherEvents(directoryItem.FileSystemWatcher, FileSystemWatcher_Changed);
                    //    break;
                }
            }
        }

        private void ConfigurationItemSetting_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Check all the cases where a setting changes requires an update, any time a copy setting or view setting is updated
            if (sender is Setting setting && (_configurationItem.CopySettings.Settings.Contains(setting) || _configurationItem.ViewSettings.Settings.Contains(setting)
                || setting.Name == nameof(_configurationItem.IncludeDirectoriesSetting)))
            {
                UpdateFileCollection();
            }

            // If the user toggled the Enabled flag, we need to reload the whole configuration
            if (sender is Setting enabledSetting && enabledSetting.Name == nameof(ConfigurationItem.EnabledSetting))
            {
                Configuration.Instance.ReloadCurrentConfiguration();
            }
        }

        private void _configurationItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ConfigurationItem.LeftButNotRightSetting):
                case nameof(ConfigurationItem.NewerOnLeftSetting):
                case nameof(ConfigurationItem.NewerOnRightSetting):
                case nameof(ConfigurationItem.FileViewOptions):
                case nameof(ConfigurationItem.EnabledSetting):
                case nameof(ConfigurationItem.ExclusionsList):
                case nameof(ConfigurationItem.IncludeDirectoriesSetting):
                    _configurationItem.GeneralSettings.Settings.ToList().ForEach(s =>
                    {
                        s.PropertyChanged -= ConfigurationItemSetting_PropertyChanged;
                        s.PropertyChanged += ConfigurationItemSetting_PropertyChanged;
                    });
                    _configurationItem.ViewSettings.Settings.ToList().ForEach(s =>
                    {
                        s.PropertyChanged -= ConfigurationItemSetting_PropertyChanged;
                        s.PropertyChanged += ConfigurationItemSetting_PropertyChanged;
                    });
                    _configurationItem.CopySettings.Settings.ToList().ForEach(s =>
                    {
                        s.PropertyChanged -= ConfigurationItemSetting_PropertyChanged;
                        s.PropertyChanged += ConfigurationItemSetting_PropertyChanged;
                    });
                    break;
            }
        }

        #endregion

        #region Private fields

        private readonly DirectoryItem _directory;

        private readonly DirectoryItem _otherDirectory;

        private readonly ConfigurationItem _configurationItem;

        private readonly DispatcherTimer _timer;

        private bool IsLeft => _configurationItem.SourceDirectories.Contains(_directory) && _configurationItem.DestinationDirectories.Contains(_otherDirectory);

        private bool IsRight => _configurationItem.DestinationDirectories.Contains(_directory) && _configurationItem.SourceDirectories.Contains(_otherDirectory);

        private CancellationTokenSource _threadCancellationTokenSource;

        #endregion

        #region Public properties

        public List<FileItem> Files {
            get => _files;
            set => Set(nameof(Files), ref _files, value);
        }
        private List<FileItem> _files = new List<FileItem>();

        public bool Dirty
        {
            get => _dirty;
            set => Set(nameof(Dirty), ref _dirty, value);
        }
        private bool _dirty;

        public string CountLabel => $"Count: {Files.OfType<InternalFileItem>().Count(f => !f.Other)}";

        #endregion

        #region IDisposable members

        public void Dispose()
        {
            //_timer.Stop();
        }

        #endregion
    }

    internal class InternalFileItem : FileItem
    {
        public InternalFileItem(FileSystemInfo fileInfo, string directory) : base(fileInfo, directory) { }

        public bool Excluded { get; set; }

        public bool Other { get; set; }

        public bool HasOther { get; set; }

        public bool Overwrite { get; set; }

        public bool GetOverwritten { get; set; }

        public override string Description
        {
            get
            {
                StringBuilder additionalDetails = new StringBuilder();

                string fileOrFolder = FileInfo is FileInfo ? "File" : FileInfo is DirectoryInfo ? "Folder" : "Item";

                if (Excluded)
                {
                    additionalDetails.Append($"{Environment.NewLine}{Environment.NewLine}{fileOrFolder} matches exclusion pattern and will not be copied.");
                }
                else if (Other)
                {
                    additionalDetails.Append($"{Environment.NewLine}{Environment.NewLine}{fileOrFolder} exists only on other side.");
                }
                else if (Overwrite)
                {
                    additionalDetails.Append($"{Environment.NewLine}{Environment.NewLine}{fileOrFolder} is newer than matching {fileOrFolder.ToLower()} on other side.");
                }
                else if (GetOverwritten)
                {
                    additionalDetails.Append($"{Environment.NewLine}{Environment.NewLine}{fileOrFolder} is older than matching {fileOrFolder.ToLower()} on other side.");
                }

                return $"{base.Description}{additionalDetails}";
            }
        }
    }

    /// <remarks>
    /// https://stackoverflow.com/a/6007538/4206279
    /// </remarks>
    internal class FileItemEqualityComparer : IEqualityComparer<FileItem>
    {
        #region IEqualityComparer members

        public bool Equals(FileItem x, FileItem y)
        {
            // TODO: The comparison type could be configurable
            return x?.Name?.ToLowerInvariant().Equals(y?.Name?.ToLowerInvariant()) == true;
        }

        public int GetHashCode(FileItem obj)
        {
            return obj.FullName.GetHashCode();
        }

        #endregion
    }

    internal class FileItemComparer : IComparer<FileItem>
    {
        #region IComparer members

        public int Compare(FileItem x, FileItem y)
        {
            // TODO: The comparison type could be configurable

            int result = default;

            if (x?.LastModifiedDateTime == y?.LastModifiedDateTime)
            {
                result = 0;
            }
            else if (x?.LastModifiedDateTime < y?.LastModifiedDateTime || x is null)
            {
                result = -1;
            }
            else if (x.LastModifiedDateTime > y?.LastModifiedDateTime || y is null)
            {
                result = 1;
            }

            return result;
        }

        #endregion
    }
}
