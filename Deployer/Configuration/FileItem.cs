#region Usings

using GalaSoft.MvvmLight;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Serialization;

#endregion

namespace Deployer
{
    public class FileItem : ObservableObject
    {
        #region Constructor

        public FileItem(FileInfo fileInfo, string directory)
        {
            FileInfo = fileInfo;
            FullName = FileInfo.FullName;
            Name = Path.GetFileName(FullName);
            Length = FileInfo.Length;
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

        public long Length { get; }

        public DateTime LastModifiedDateTime { get; }

        public FileInfo FileInfo { get; }

        public virtual string Description => $"Full path: {FullName}{Environment.NewLine}Size: {Length} bytes{Environment.NewLine}Last modified at: {LastModifiedDateTime}";

        public bool CancelLoad { get; set; }

        #endregion
    }
}
