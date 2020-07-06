#region Usings

using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

#endregion

namespace Deployer
{
    [Serializable]
    public class DirectoryItem : ObservableObject
    {
        #region Constructor

        public DirectoryItem()
        {
            PropertyChanged += DirectoryItem_PropertyChanged;
        }

        #endregion

        #region Event handlers

        private void DirectoryItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Path):
                    //Utilities.FileSystem.UnregisterFileSystemWatcher(FileSystemWatcher);
                    //FileSystemWatcher = Utilities.FileSystem.RegisterFileSystemWatcher(Path);
                    break;
            }
        }

        #endregion

        #region Public methods

        public IEnumerable<FileInfo> GetFileInfos()
        {
            return Native.DirectoryExists(Path) ? DirectoryInfo.GetFiles() : Enumerable.Empty<FileInfo>();
        }

        #endregion

        #region Public properties

        public string RawPath
        {
            get => _path;
            set
            {
                Set(nameof(Path), ref _path, value);
                RaisePropertyChanged(nameof(RawPath));
                RaisePropertyChanged(nameof(Path));
            }
        }
        private string _path;

        [XmlIgnore]
        public string Path
        {
            get
            {
                string resolvedPath = RawPath;
                if (!string.IsNullOrEmpty(resolvedPath) && Configuration.Instance?.PathVariables is { })
                {
                    foreach (PathVariable pathVariable in Configuration.Instance.PathVariables)
                    {
                        resolvedPath = resolvedPath.Replace(pathVariable.Name, pathVariable.SelectedValue.Value);
                    }
                }

                return resolvedPath;
            }
            set => RawPath = value;
        }

        private DirectoryInfo DirectoryInfo => new DirectoryInfo(Path);

        //[XmlIgnore]
        //public FileSystemWatcher FileSystemWatcher
        //{
        //    get => _fileSystemWatcher;
        //    set => Set(nameof(FileSystemWatcher), ref _fileSystemWatcher, value);
        //}
        //private FileSystemWatcher _fileSystemWatcher;

        #endregion
    }
}
