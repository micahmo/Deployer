#region Usings

using System.ComponentModel;
using GalaSoft.MvvmLight;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using GuiLibraryInterfaces;
using System.Linq;
using Point = System.Windows.Point;
using System.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows.Media;
using System.Threading;
using Timer = System.Timers.Timer;

#endregion

namespace Deployer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = Model ??= new MainWindowModel(this);
            Model.Configuration = Configuration.Load();

            leftTabControl.SizeChanged += TabControl_SizeChanged;
            rightTabControl.SizeChanged += TabControl_SizeChanged;

            Timer autoSaveTimer = new Timer {Interval = TimeSpan.FromMinutes(1).TotalMilliseconds};
            autoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;
            autoSaveTimer.Start();
        }

        #region Private fields/properties

        private MainWindowModel Model { get; }

        #endregion

        #region Event handlers

        private void AutoSaveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Save();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Save();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is TabControl tabControl && tabControl.SelectedIndex < 0)
            {
                Model.LeftSelectedDirectoryPair = Model.RightSelectedDirectoryPair = Model.SelectedConfigurationItem?.DirectoryPairs.FirstOrDefault();
            }
        }

        private void FilesDataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            foreach (FilesDataGrid gridView in this.FindVisualChildren<FilesDataGrid>())
            {
                foreach (ScrollViewer scrollViewer in gridView.FindVisualChildren<ScrollViewer>())
                {
                    scrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
                }
            }
        }

        private void ReloadCurrentConfigurationCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Configuration.Instance.ReloadCurrentConfiguration();
        }

        private void DeployCurrentConfigurationCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Model.Commands.DeployCommand?.Execute(null);
        }

        private void TabControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Model.RaisePropertyChanged(nameof(Model.LeftTabControlMaxWidth));
            Model.RaisePropertyChanged(nameof(Model.RightTabControlMaxWidth));
        }

        private void GoToConfigurationITem_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        #endregion

        #region Private methods

        private void Save()
        {
            // Have to retrieve window properties in UI thread
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Have to manually save readonly properties which are not bindable
                Model.Configuration.WindowSize = new Size(ActualWidth, ActualHeight);
                Model.Configuration.WindowLocation = new Point(Left, Top);
                Model.Configuration.ConfigurationItemsWidth = ConfigurationColumn.ActualWidth;

                Configuration.Save(Model.Configuration);
            });
        }

        #endregion
    }

    internal class MainWindowModel : ObservableObject
    {
        public MainWindowModel(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            Commands = new MainWindowCommands(this, _mainWindow);
        }

        private readonly MainWindow _mainWindow;
        
        public MainWindowCommands Commands { get; }

        public Configuration Configuration
        {
            get => _configuration;
            set => Set(nameof(Configuration), ref _configuration, value);
        }
        private Configuration _configuration;

        public ConfigurationItem SelectedConfigurationItem
        {
            get => _selectedConfigurationItem;
            set => Set(nameof(SelectedConfigurationItem), ref _selectedConfigurationItem, value);
        }
        private ConfigurationItem _selectedConfigurationItem;

        public PathVariable SelectedPathVariable
        {
            get => _selectedPathVariable;
            set => Set(nameof(SelectedPathVariable), ref _selectedPathVariable, value);
        }
        private PathVariable _selectedPathVariable;

        //public FileItem SelectedFileItem
        //{
        //    get => _selectedFileItem;
        //    set => Set(nameof(SelectedFileItem), ref _selectedFileItem, value);
        //}
        //private FileItem _selectedFileItem;

        public Thickness SourceDirectoriesMargin
        {
            get => _sourceDirectoriesMargin;
            set => Set(nameof(SourceDirectoriesMargin), ref _sourceDirectoriesMargin, value);
        }
        private Thickness _sourceDirectoriesMargin;

        public Thickness DestinationDirectoriesMargin {
            get => _destinationDirectoriesMargin;
            set => Set(nameof(DestinationDirectoriesMargin), ref _destinationDirectoriesMargin, value);
        }
        private Thickness _destinationDirectoriesMargin;

        public int TabControlSelectedIndex
        {
            get => _tabControlSelectedIndex;
            set => Set(nameof(TabControlSelectedIndex), ref _tabControlSelectedIndex, value);
        }
        private int _tabControlSelectedIndex;

        public DirectoryPair LeftSelectedDirectoryPair
        {
            get => _leftSelectedDirectoryPair;
            set => Set(nameof(LeftSelectedDirectoryPair), ref _leftSelectedDirectoryPair, value);
        }
        private DirectoryPair _leftSelectedDirectoryPair;

        public DirectoryPair RightSelectedDirectoryPair {
            get => _rightSelectedDirectoryPair;
            set => Set(nameof(RightSelectedDirectoryPair), ref _rightSelectedDirectoryPair, value);
        }
        private DirectoryPair _rightSelectedDirectoryPair;

        public double LeftTabControlMaxWidth => _mainWindow.leftTabControl.ActualWidth - (_mainWindow.leftTabControlCountLabel.ActualWidth + 50);

        public double RightTabControlMaxWidth => _mainWindow.rightTabControl.ActualWidth - (_mainWindow.rightTabControlCountLabel.ActualWidth + 50);

        public string DeployButtonTooltip => $"Deploy ({ShortcutCommands.GetShortcutKey(ShortcutCommands.DeployCurrentConfigurationCommand).FirstOrDefault()})";

        public bool IsBusy
        {
            get => _isBusy;
            set => Set(nameof(IsBusy), ref _isBusy, value);
        }
        private bool _isBusy;

        public string DeployStep
        {
            get => _deployStep;
            set => Set(nameof(DeployStep), ref _deployStep, value);
        }
        private string _deployStep;

        public string DeployDetails {
            get => _deployDetails;
            set => Set(nameof(DeployDetails), ref _deployDetails, value);
        }
        private string _deployDetails;

        public bool IndeterminateProgress
        {
            get => _indeterminateProgress;
            set => Set(nameof(IndeterminateProgress), ref _indeterminateProgress, value);
        }
        private bool _indeterminateProgress;

        public double DeployProgress
        {
            get => _deployProgress;
            set => Set(nameof(DeployProgress), ref _deployProgress, value);
        }
        private double _deployProgress;

        public string DeployUnhandledError
        {
            get => _deployUnhandledError;
            set => Set(nameof(DeployUnhandledError), ref _deployUnhandledError, value);
        }
        private string _deployUnhandledError;

        public string DeployUnhandledErrorDetails
        {
            get => _deployUnhandledErrorDetails;
            set => Set(nameof(DeployUnhandledErrorDetails), ref _deployUnhandledErrorDetails, value);
        }
        private string _deployUnhandledErrorDetails;

        public bool DeployEncounteredUnhandledError
        {
            get => _deployEncounteredUnhandledError;
            set => Set(nameof(DeployEncounteredUnhandledError), ref _deployEncounteredUnhandledError, value);
        }
        private bool _deployEncounteredUnhandledError;

        public bool DeployInProgress
        {
            get => _deployInProgress;
            set => Set(nameof(DeployInProgress), ref _deployInProgress, value);
        }
        private bool _deployInProgress;

        public bool DeployEncounteredHandledErrors
        {
            get => _deployEncounteredHandledErrors;
            set => Set(nameof(DeployEncounteredHandledErrors), ref _deployEncounteredHandledErrors, value);
        }
        private bool _deployEncounteredHandledErrors;

        public string DeployHandledErrors
        {
            get => _deployHandledErrors;
            set => Set(nameof(DeployHandledErrors), ref _deployHandledErrors, value);
        }
        private string _deployHandledErrors;

        public string DeployHandledErrorsDetails {
            get => _deployHandledErrorsDetails;
            set => Set(nameof(DeployHandledErrorsDetails), ref _deployHandledErrorsDetails, value);
        }
        private string _deployHandledErrorsDetails = string.Empty;

        public bool CancelRequested
        {
            get => _cancelRequested;
            set => Set(nameof(CancelRequested), ref _cancelRequested, value);
        }
        private bool _cancelRequested;
    }

    internal class MainWindowCommands
    {
        #region Constructor

        public MainWindowCommands(MainWindowModel model, MainWindow mainWindow)
            => (Model, MainWindow) = (model, mainWindow);

        #endregion

        #region Private properties

        private MainWindowModel Model { get; }

        private MainWindow MainWindow { get; }

        #endregion

        #region ICommands

        public ICommand AddConfigurationItemCommand => _addConfigurationItemCommand ??= new RelayCommand(AddConfigurationItem);
        private RelayCommand _addConfigurationItemCommand;

        public ICommand RemoveConfigurationItemCommand => _removeConfigurationItemCommand ??= new RelayCommand(RemoveConfigurationItem);
        private RelayCommand _removeConfigurationItemCommand;

        public ICommand AddPathVariableCommand => _addPathVariableCommand ??= new RelayCommand(AddPathVariable);
        private RelayCommand _addPathVariableCommand;

        public ICommand RemovePathVariableCommand => _removePathVariableCommand ??= new RelayCommand(RemovePathVariable);
        private RelayCommand _removePathVariableCommand;

        public ICommand DeployCommand => _deployCommand ??= new RelayCommand(Deploy);
        private RelayCommand _deployCommand;

        public ICommand CloseProgressCommand => _closeProgressCommand ??= new RelayCommand(CloseProgress);
        private RelayCommand _closeProgressCommand;

        public ICommand CancelDeployCommand => _cancelDeployCommand ??= new RelayCommand(CancelDeploy);
        private RelayCommand _cancelDeployCommand;

        public ICommand ViewLastErrorCommand => _viewLastErrorCommand ??= new RelayCommand(ViewLastError);
        private RelayCommand _viewLastErrorCommand;

        #endregion

        #region Command implementations

        private void AddConfigurationItem()
        {
            Model.Configuration.ConfigurationItems.Add(new ConfigurationItem {Name = Model.Configuration.GenerateNewConfigurationItemName()}.GenerateInitialDirectoryLists());
            Model.SelectedConfigurationItem = Model.Configuration.ConfigurationItems.Last();
        }

        private void RemoveConfigurationItem()
        {
            if (Model.SelectedConfigurationItem is { })
            {
                if (QuestionResult.Yes == Dependencies.Notify.Question(string.Format(Resource.ConfirmDeleteConfiguration, Model.SelectedConfigurationItem.Name), Resource.Question, QuestionOptions.YesNo))
                {
                    Model.Configuration.ConfigurationItems.Remove(Model.SelectedConfigurationItem);
                }
            }
        }

        private void AddPathVariable()
        {
            Model.Configuration.PathVariables.Add(new PathVariable {Name = "%%"}.GenerateInitialValuesLists());
        }

        private void RemovePathVariable()
        {
            if (Model.SelectedPathVariable is { })
            {
                Model.Configuration.PathVariables.Remove(Model.SelectedPathVariable);
            }
        }

        private async void Deploy()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            int errors = 0;

            Model.DeployStep = "Preparing to Deploy";
            Model.DeployDetails = string.Empty;
            Model.IndeterminateProgress = true;
            Model.DeployEncounteredUnhandledError = false;
            Model.DeployUnhandledError = string.Empty;
            Model.DeployUnhandledErrorDetails = string.Empty;
            Model.DeployEncounteredHandledErrors = false;
            Model.DeployHandledErrors = string.Empty;
            Model.DeployHandledErrorsDetails = string.Empty;
            Model.CancelRequested = false;
            Model.DeployInProgress = true;
            Model.IsBusy = true;

            Configuration.Instance.ReloadCurrentConfiguration();
            
            DeploymentItem deploymentItem = await Configuration.Instance.PrepareDeployment();

            if (deploymentItem.FilesToCopy.Count > 0)
            {
                Model.IndeterminateProgress = false;
                Model.DeployStep = "Copying Files";

                Progress<DeployProgress> progress = new Progress<DeployProgress>(e =>
                {
                    Model.DeployStep = e.CurrentStep;
                    Model.DeployDetails = e.Details;
                    Model.DeployProgress = e.PercentComplete ?? Model.DeployProgress;
                });

                Progress<DeployError> errorProgress = new Progress<DeployError>(e =>
                {
                    ++errors;

                    Model.DeployEncounteredHandledErrors = true;
                    Model.DeployHandledErrors = $"Deploy encountered {errors} errors(s).";
                    Model.DeployHandledErrorsDetails += $"{e.Details}{Environment.NewLine}{Environment.NewLine}";
                });

                try
                {
                    await Configuration.Instance.Deploy(deploymentItem, _cancellationTokenSource, progress, errorProgress);
                }
                catch (Exception ex)
                {
                    ++errors;

                    Model.DeployUnhandledError = $"Error: {ex.Message}";
                    Model.DeployUnhandledErrorDetails = ex.ToString();
                    Model.DeployInProgress = false;
                    Model.DeployEncounteredUnhandledError = true;
                }

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    Model.DeployUnhandledError = "The operation was canceled.";
                    Model.DeployInProgress = false;
                    Model.DeployEncounteredUnhandledError = true;
                }

                Configuration.Instance.ReloadCurrentConfiguration();
            }

            Model.DeployInProgress = false;
            Model.IsBusy = errors > 0 && !Configuration.Instance.CloseDialogOnErrors;
        }

        private CancellationTokenSource _cancellationTokenSource;

        private void CloseProgress()
        {
            Model.IsBusy = false;
        }

        private void CancelDeploy()
        {
            Model.CancelRequested = true;
            _cancellationTokenSource?.Cancel();
        }

        private void ViewLastError()
        {
            Model.IsBusy = true;
        }

        #endregion
    }

    internal static class WpfExtensions
    {
        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
}
