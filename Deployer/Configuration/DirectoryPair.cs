#region Usings

using System;
using System.ComponentModel;
using System.IO;
using GalaSoft.MvvmLight;

#endregion

namespace Deployer
{
    public class DirectoryPair : ObservableObject
    {
        public DirectoryPair(DirectoryItem left, DirectoryItem right, ConfigurationItem configurationItem)
        {
            Left = left;
            Right = right;

            _configurationItem = configurationItem;

            LeftFileCollection = new FileCollection(left, right, _configurationItem);
            RightFileCollection = new FileCollection(right, left, _configurationItem);

            Left.PropertyChanged += DirectoryItem_PropertyChanged;
            Right.PropertyChanged += DirectoryItem_PropertyChanged;
        }

        private void DirectoryItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(DirectoryItem.Path):
                    RaisePropertyChanged(nameof(Name));
                    RaisePropertyChanged(nameof(ShortName));
                    break;
            }
        }

        public DirectoryItem Left { get; }

        public DirectoryItem Right { get; }

        public FileCollection LeftFileCollection { get; }

        public FileCollection RightFileCollection { get; }

        public string Name => $"{Left.Path}{Environment.NewLine}{(char)8645}{Environment.NewLine}{Right.Path}";

        public string ShortName => _configurationItem.EnabledSetting.Value
                                   ? $"{(Native.DirectoryExists(Left.Path) ? new DirectoryInfo(Left.Path).Name : string.Empty)} {(char) 8644} " +
                                     $"{(Native.DirectoryExists(Right.Path) ? new DirectoryInfo(Right.Path).Name : string.Empty)}"
                                   : string.Empty;

        private readonly ConfigurationItem _configurationItem;
    }
}
