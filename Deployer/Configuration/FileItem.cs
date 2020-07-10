#region Usings

using GalaSoft.MvvmLight;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;

#endregion

namespace Deployer
{
    public class FileItem : ObservableObject
    {
        #region Constructor

        public FileItem(FileSystemInfo fileInfo, string directory)
        {
            FileInfo = fileInfo;
            FullName = FileInfo.FullName;
            Name = Path.GetFileName(FullName);
            Length = (fileInfo as FileInfo)?.Length;
            LastModifiedDateTime = File.GetLastWriteTime(FullName);
            PropertyChanged += FileItem_PropertyChanged;
        }

        #endregion

        #region Event handlers

        private void FileItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(FullName):
                case nameof(Length):
                case nameof(LastModifiedDateTime):
                    RaisePropertyChanged(nameof(Description));
                    break;
            }
        }

        #endregion

        #region Public properties

        public ImageSource Icon {
            get => _icon;
            set => Set(nameof(Icon), ref _icon, value);
        }
        public ImageSource _icon;

        public string Name {
            get => _name;
            set => Set(nameof(Name), ref _name, value);
        }

        private string _name;

        public string FullName {
            get => _fullName;
            set => Set(nameof(FullName), ref _fullName, value);
        }
        private string _fullName;

        public long? Length { get; }

        public DateTime LastModifiedDateTime { get; }

        public FileSystemInfo FileInfo { get; }

        public virtual string Description => $"Full path: {FullName}{Environment.NewLine}Size: {Length?.ToString() ?? "unknown"} bytes{Environment.NewLine}Last modified at: {LastModifiedDateTime}";

        public bool IsDirectory => FileInfo is DirectoryInfo;

        #endregion
    }
}
