#region Usings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

#endregion

namespace Deployer
{
    /// <summary>
    /// Interaction logic for DirectoryEditor.xaml
    /// </summary>
    public partial class DirectoryEditor : UserControl
    {
        #region Constructor

        public DirectoryEditor()
        {
            InitializeComponent();
        }

        private DirectoryItem DataModel => DataContext as DirectoryItem;

        public DirectoryEditorModel ViewModel { get; } = new DirectoryEditorModel();

        #endregion

        #region Dependency properties

        public static readonly DependencyProperty CurrentDirectoryProperty = DependencyProperty.Register(nameof(CurrentDirectory), typeof(string), typeof(DirectoryEditor), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, CurrentDirectoryChangedCallback));

        public static readonly DependencyProperty TopDirectoryProperty = DependencyProperty.Register(nameof(TopDirectory), typeof(object), typeof(DirectoryEditor), new PropertyMetadata(null, TopDirectoryChangedCallback));

        public static readonly DependencyProperty DirectoryCountProperty = DependencyProperty.Register(nameof(DirectoryCount), typeof(int), typeof(DirectoryEditor), new PropertyMetadata(0, DirectoryCountChanged));

        #endregion

        #region Public properties

        public string CurrentDirectory
        {
            get => (string)GetValue(CurrentDirectoryProperty);
            set => SetValue(CurrentDirectoryProperty, value);
        }

        public object TopDirectory
        {
            get => GetValue(TopDirectoryProperty);
            set => SetValue(TopDirectoryProperty, value);
        }

        public int DirectoryCount
        {
            get => (int)GetValue(DirectoryCountProperty);
            set => SetValue(DirectoryCountProperty, value);
        }

        #endregion

        #region Public events

        public event EventHandler<DirectoryRemovedEventArgs> DirectoryRemoved;

        public event EventHandler DirectoryAdded;

        #endregion

        #region Internal methods

        internal void OnDirectoryRemoved()
        {
            DirectoryRemoved?.Invoke(this, new DirectoryRemovedEventArgs(DataModel));
        }

        internal void OnDirectoryAdded()
        {
            DirectoryAdded?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Event handlers

        private static void TopDirectoryChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DirectoryEditor control)
            {
                control.ViewModel.ShowAddDirectoryButton = control.DataModel == control.TopDirectory;
            }
        }

        private static void DirectoryCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DirectoryEditor control)
            {
                control.ViewModel.ShowRemoveDirectoryButton = control.DirectoryCount > 1;
            }
        }

        private static void CurrentDirectoryChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DirectoryEditor control)
            {
                control.ViewModel.ShowPathWarning = Configuration.Instance is { } // The configuration was loaded, so we know the variables have been resolved
                                                    && Configuration.GetConfigurationItem(control.DataModel)?.EnabledSetting.Value == true // This item is enabled
                                                    && !string.IsNullOrEmpty(control.CurrentDirectory) // The user has entered a non-empty valid string for the path
                                                    && !Native.DirectoryExists(control.CurrentDirectory); // The path exists
            }
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(CurrentDirectory);
            }
            catch
            {
                // If the path does not exist, the Process.Start will throw an exception.
                // However, we'd rather let the user try and catch the exception
                // than spend the time to check whether the path exists upfront
            }
        }

        #endregion
    }

    public class DirectoryRemovedEventArgs : EventArgs
    {
        public DirectoryRemovedEventArgs(DirectoryItem directoryItem) => RemovedDirectory = directoryItem;

        public DirectoryItem RemovedDirectory { get; }
    }

    public class DirectoryEditorModel : ObservableObject
    {
        public DirectoryEditorModel()
        {
            Commands = new DirectoryEditorCommands();
        }

        public DirectoryEditorCommands Commands { get; }

        public bool ShowAddDirectoryButton
        {
            get => _showAddDirectoryButton;
            set => Set(nameof(ShowAddDirectoryButton), ref _showAddDirectoryButton, value);
        }
        private bool _showAddDirectoryButton;

        public bool ShowRemoveDirectoryButton
        {
            get => _showRemoveDirectoryButton;
            set => Set(nameof(ShowRemoveDirectoryButton), ref _showRemoveDirectoryButton, value);
        }
        private bool _showRemoveDirectoryButton = true;

        public bool ShowPathWarning
        {
            get => _showPathWarning;
            set => Set(nameof(ShowPathWarning), ref _showPathWarning, value);
        }
        private bool _showPathWarning;

        public IEnumerable<string> OtherPaths
        {
            get
            {
                HashSet<string> otherPaths = new HashSet<string>();

                Configuration.Instance?.ConfigurationItems.ToList().ForEach(c =>
                {
                    otherPaths.UnionWith(c.SourceDirectories.Select(d => d.RawPath).Where(s => !string.IsNullOrEmpty(s)));
                    otherPaths.UnionWith(c.DestinationDirectories.Select(d => d.RawPath).Where(s => !string.IsNullOrEmpty(s)));
                });

                return otherPaths;
            }
        }
    }

    public class DirectoryEditorCommands
    {
        #region ICommands

        public ICommand BrowseForDirectoryCommand => _browseForDirectoryCommand ??= new RelayCommand<DirectoryEditor>(BrowseForDirectory);
        private RelayCommand<DirectoryEditor> _browseForDirectoryCommand;

        public ICommand RemoveDirectoryCommand => _removeDirectoryCommand ??= new RelayCommand<DirectoryEditor>(RemoveDirectory);
        private RelayCommand<DirectoryEditor> _removeDirectoryCommand;

        public ICommand AddDirectoryCommand => _addDirectoryCommand ??= new RelayCommand<DirectoryEditor>(AddDirectory);
        private RelayCommand<DirectoryEditor> _addDirectoryCommand;

        #endregion

        #region Implementations

        private void BrowseForDirectory(DirectoryEditor control)
        {
            if (Dependencies.FileBrowser.BrowseForDirectory(control.CurrentDirectory) is { } directory)
            {
                control.CurrentDirectory = directory;
            }
        }

        private void RemoveDirectory(DirectoryEditor control)
        {
            control.OnDirectoryRemoved();
        }

        private void AddDirectory(DirectoryEditor control)
        {
            control.OnDirectoryAdded();
        }

        #endregion
    }
}
