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
using System.IO;
using System.Timers;
using System.Windows.Media;
using System.Threading;
using System.Windows.Threading;
using Bluegrams.Application;
using Bluegrams.Application.WPF;
using Deployer.Properties;
using HTMLConverter;
using Microsoft.Extensions.DependencyInjection;
using Utilities;
using Timer = System.Timers.Timer;
using Xctk = Xceed.Wpf.Toolkit;

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
            // Ensure that closing the MainWindow also closes any child windows and stops the application.
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

            InitializeComponent();
            DataContext = Model ??= new MainWindowModel(this);
            Model.Configuration = Configuration.Load();

            leftTabControl.SizeChanged += TabControl_SizeChanged;
            rightTabControl.SizeChanged += TabControl_SizeChanged;

            UpdateChecker = new MyUpdateChecker("https://raw.githubusercontent.com/micahmo/Deployer/master/Deployer/VersionInfo.xml")
            {
                Owner = this,
                DownloadIdentifier = "portable"
            };

            Timer autoSaveTimer = new Timer {Interval = TimeSpan.FromMinutes(1).TotalMilliseconds};
            autoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;
            autoSaveTimer.Start();
        }

        #region Private fields/properties

        private MainWindowModel Model { get; }

        #endregion

        #region Public properties

        public WpfUpdateChecker UpdateChecker { get; }

        #endregion

        #region Event handlers

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateChecker.CheckForUpdates();
        }

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

        private void HardReloadCurrentConfigurationCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Configuration.Instance.HardReloadCurrentConfiguration();
        }

        private void DeployCurrentConfigurationCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Model.Commands.DeployCommand?.Execute(null);
        }

        private void PreviousConfigurationCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Model.Configuration.SelectedConfigurationIndex = Math.Max(Model.Configuration.SelectedConfigurationIndex - 1, 0);
        }

        private void NextConfigurationCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Model.Configuration.SelectedConfigurationIndex = Math.Min(Model.Configuration.SelectedConfigurationIndex + 1, Model.Configuration.ConfigurationItems.Count - 1);
        }

        private void MoveConfigurationUpCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Model.Commands.MoveConfigurationItemUpCommand?.Execute(null);
        }

        private void MoveConfigurationDownCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Model.Commands.MoveConfigurationItemDownCommand?.Execute(null);
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Model.Commands.ShowAboutCommand?.Execute(null);
        }

        private void TabControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Model.RaisePropertyChanged(nameof(Model.LeftTabControlMaxWidth));
            Model.RaisePropertyChanged(nameof(Model.RightTabControlMaxWidth));
        }

        private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Model.RaisePropertyChanged(nameof(Model.BusyIndicatorWidth));
            Model.RaisePropertyChanged(nameof(Model.LogWidth));
            Model.RaisePropertyChanged(nameof(Model.LogHeight));
        }

        private void PathVariableGrid_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // The user's focus has moved out of the path variables area, so we can deselect any selected variables
            if (PathVariableGridContainer.FindVisualChildren<Control>().Contains(e.NewFocus) == false)
            {
                PathVariableGrid.UnselectAll();
            }
        }

        private void ViewLogButton_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            DeployButton.IsOpen = false; // For some reason, Binding doesn't work on this property, so I have to set it directly
            Model.RaisePropertyChanged(nameof(Model.Log));
            Model.ShowLog = true;
        }

        private void LogTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Xctk.RichTextBox richTextBox && (bool)e.NewValue)
            {
                // Scroll with a low priority to guarantee that the full text has loaded first
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    richTextBox.ScrollToEnd();
                    Mouse.OverrideCursor = null;
                }), DispatcherPriority.ApplicationIdle);
            }
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
                Model.Configuration.ConfigurationItemsHeight = new GridLength(ConfigurationRow.ActualHeight / PathVariablesRow.ActualHeight, GridUnitType.Star);

                Configuration.Save(Model.Configuration);
            });
        }

        #endregion
    }

    #region MainWindowModel class

    internal class MainWindowModel : ObservableObject
    {
        #region Constructor

        public MainWindowModel(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            Commands = new MainWindowCommands(this, _mainWindow);
        }

        #endregion

        #region Properties/fields

        private readonly MainWindow _mainWindow;

        public MainWindowCommands Commands { get; }

        #endregion

        #region Observable Properties

        public string Title => string.Format(Resources.DeployerTitle, AppInfo.Version);

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

        public Thickness DestinationDirectoriesMargin
        {
            get => _destinationDirectoriesMargin;
            set => Set(nameof(DestinationDirectoriesMargin), ref _destinationDirectoriesMargin, value);
        }
        private Thickness _destinationDirectoriesMargin;

        public DirectoryPair LeftSelectedDirectoryPair
        {
            get => _leftSelectedDirectoryPair;
            set => Set(nameof(LeftSelectedDirectoryPair), ref _leftSelectedDirectoryPair, value);
        }
        private DirectoryPair _leftSelectedDirectoryPair;

        public DirectoryPair RightSelectedDirectoryPair
        {
            get => _rightSelectedDirectoryPair;
            set => Set(nameof(RightSelectedDirectoryPair), ref _rightSelectedDirectoryPair, value);
        }
        private DirectoryPair _rightSelectedDirectoryPair;

        public double LeftTabControlMaxWidth => _mainWindow.leftTabControl.ActualWidth - (_mainWindow.leftTabControlCountLabel.ActualWidth + 50);

        public double RightTabControlMaxWidth => _mainWindow.rightTabControl.ActualWidth - (_mainWindow.rightTabControlCountLabel.ActualWidth + 50);

        public string DeployButtonTooltip => string.Format(Resources.DeployTooltip, ShortcutCommands.GetShortcutKey(ShortcutCommands.DeployCurrentConfigurationCommand).FirstOrDefault());

        public bool ShowLog
        {
            get => _showLog;
            set => Set(nameof(ShowLog), ref _showLog, value);
        }
        private bool _showLog;

        public string Log => HtmlToXamlConverter.ConvertHtmlToXaml(File.ReadAllText(App.ServiceProvider.GetRequiredService<SessionFileLogger>().Path), false);

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

        public double BusyIndicatorWidth => Math.Min(_mainWindow.ActualWidth - 100, 600);

        public double LogWidth => _mainWindow.ActualWidth - 300;

        public double LogHeight => _mainWindow.ActualHeight - 300;

        #endregion
    }

    #endregion

    #region MainWindowCommands class

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

        public ICommand CloseLogCommand => _closeLogCommand ??= new RelayCommand(CloseLog);
        private RelayCommand _closeLogCommand;

        public ICommand ClearLogCommand => _clearLogCommand ??= new RelayCommand(ClearLog);
        private RelayCommand _clearLogCommand;

        public ICommand CancelDeployCommand => _cancelDeployCommand ??= new RelayCommand(CancelDeploy);
        private RelayCommand _cancelDeployCommand;

        public ICommand ViewLastErrorCommand => _viewLastErrorCommand ??= new RelayCommand(ViewLastError);
        private RelayCommand _viewLastErrorCommand;

        public ICommand MoveConfigurationItemUpCommand => _moveConfigurationItemUpCommand ??= new RelayCommand(MoveConfigurationItemUp);
        private RelayCommand _moveConfigurationItemUpCommand;

        public ICommand MoveConfigurationItemDownCommand => _moveConfigurationItemDownCommand ??= new RelayCommand(MoveConfigurationItemDown);
        private RelayCommand _moveConfigurationItemDownCommand;

        public ICommand ShowAboutCommand => _showAboutCommand ??= new RelayCommand(ShowAbout);
        private RelayCommand _showAboutCommand;

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
                if (QuestionResult.Yes == App.ServiceProvider.GetRequiredService<INotify>()
                    .Question(string.Format(Resources.ConfirmDeleteConfiguration, Model.SelectedConfigurationItem.Name), Resources.Question, QuestionOptions.YesNo))
                {
                    int previousSelectedIndex = Model.Configuration.SelectedConfigurationIndex;

                    // Remove references to this Configuration Item in other items
                    Model.Configuration.ConfigurationItems
                        .Where(c => c.NextConfigurationSetting.Value.Equals(Model.SelectedConfigurationItem.GuidInfo))
                        .ToList().ForEach(c => c.NextConfigurationSetting.Value = c.NextConfigurationSetting.DefaultValue);
                    
                    // Delete the config
                    Model.Configuration.ConfigurationItems.Remove(Model.SelectedConfigurationItem);

                    // Select the previous item in the list
                    if (Model.Configuration.ConfigurationItems.Any())
                    {
                        Model.Configuration.SelectedConfigurationIndex = Math.Max(0, previousSelectedIndex - 1);
                    }
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
            if (Model.DeployInProgress)
            {
                return;
            }

            Model.DeployInProgress = true;

            _cancellationTokenSource = new CancellationTokenSource();
            int errors = 0;

            Model.DeployStep = Resources.PreparingToDeploy;
            Model.DeployDetails = string.Join(Environment.NewLine, string.Empty, string.Empty, string.Empty);
            Model.IndeterminateProgress = true;
            Model.DeployEncounteredUnhandledError = false;
            Model.DeployUnhandledError = string.Empty;
            Model.DeployUnhandledErrorDetails = string.Empty;
            Model.DeployEncounteredHandledErrors = false;
            Model.DeployHandledErrors = string.Empty;
            Model.DeployHandledErrorsDetails = string.Empty;
            Model.CancelRequested = false;
            Model.IsBusy = true;

            LogManager.LogBreak();

            ConfigurationItem configurationToDeploy = Model.SelectedConfigurationItem;

            while (configurationToDeploy is { } && _cancellationTokenSource.IsCancellationRequested == false)
            {
                Model.SelectedConfigurationItem = configurationToDeploy;
                Configuration.Instance.ReloadCurrentConfiguration();

                DeploymentItem deploymentItem = await Configuration.Instance.PrepareDeployment();

                if (deploymentItem.FilesToCopy.Count > 0)
                {
                    Model.IndeterminateProgress = false;
                    Model.DeployStep = Resources.CopyingFilesTitle;

                    LogManager.Log(Resources.BeginningDeployment);

                    Progress<DeployProgress> progress = new Progress<DeployProgress>(e =>
                    {
                        Model.DeployDetails = e.Details;

                        // Make sure the details text is at least 3 lines
                        for (int i = e.Details.Lines().Length; i < 3; ++i)
                        {
                            Model.DeployDetails += Environment.NewLine;
                        }

                        Model.DeployStep = e.CurrentStep;
                        Model.DeployProgress = e.PercentComplete ?? Model.DeployProgress;

                        LogManager.Log(e.Details, e.CurrentStep);
                    });

                    Progress<DeployError> errorProgress = new Progress<DeployError>(e =>
                    {
                        ++errors;

                        Model.DeployEncounteredHandledErrors = true;
                        Model.DeployHandledErrors = string.Format(Resources.DeployEncounteredErrors, errors);
                        Model.DeployHandledErrorsDetails += string.Join(Environment.NewLine, e.Details, string.Empty, string.Empty);

                        LogManager.Log(e.Details, Model.DeployStep, LogLevel.Error);
                    });

                    try
                    {
                        await Configuration.Instance.Deploy(deploymentItem, _cancellationTokenSource, progress, errorProgress);
                    }
                    catch (Exception ex)
                    {
                        ++errors;

                        Model.DeployUnhandledError = string.Format(Resources.Error, ex.Message);
                        Model.DeployUnhandledErrorDetails = ex.ToString();
                        Model.DeployInProgress = false;
                        Model.DeployEncounteredUnhandledError = true;

                        LogManager.Log(ex.ToString(), Resources.UnhandledError, LogLevel.Error);
                    }

                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        Model.DeployUnhandledError = Resources.TheOperationWasCanceled;
                        Model.DeployInProgress = false;
                        Model.DeployEncounteredUnhandledError = true;

                        LogManager.Log(Resources.TheOperationWasCanceled, level: LogLevel.Error);
                    }

                    Model.DeployStep = Resources.FinishedDeployment;
                    Model.DeployDetails = string.Join(Environment.NewLine, string.Empty, string.Empty, string.Empty);
                    LogManager.Log(Resources.FinishedDeployment);

                    Configuration.Instance.ReloadCurrentConfiguration();
                }
                else
                {
                    LogManager.Log(Resources.NoOperation);
                }

                configurationToDeploy = 
                    Model.SelectedConfigurationItem.NextConfigurationSetting.OptionSelected 
                        ? Model.SelectedConfigurationItem.NextConfigurationSetting.Value.GetConfigurationItem() 
                        : null;
            }

            Model.IsBusy = errors > 0 && !Configuration.Instance.CloseDialogOnErrors;
            Model.DeployInProgress = false;
        }

        private CancellationTokenSource _cancellationTokenSource;

        private void CloseProgress()
        {
            Model.IsBusy = false;
        }

        private void CloseLog()
        {
            Model.ShowLog = false;
        }

        private void ClearLog()
        {
            App.ServiceProvider.GetRequiredService<SessionFileLogger>().Clear();
            Model.RaisePropertyChanged(nameof(Model.Log));
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

        private void MoveConfigurationItemUp()
        {
            if (Model.Configuration.ConfigurationItems.Count > 1 && Model.Configuration.SelectedConfigurationIndex > 0)
            {
                int selectedConfigurationIndex = Model.Configuration.SelectedConfigurationIndex;
                ConfigurationItem configurationItem = Model.Configuration.ConfigurationItems[selectedConfigurationIndex];
                
                Model.Configuration.ConfigurationItems.RemoveAt(selectedConfigurationIndex);
                Model.Configuration.ConfigurationItems.Insert(selectedConfigurationIndex - 1, configurationItem);

                Model.Configuration.SelectedConfigurationIndex = Model.Configuration.ConfigurationItems.IndexOf(configurationItem);
            }
        }

        private void MoveConfigurationItemDown()
        {
            if (Model.Configuration.ConfigurationItems.Count > 1 && Model.Configuration.SelectedConfigurationIndex < Model.Configuration.ConfigurationItems.Count)
            {
                int selectedConfigurationIndex = Model.Configuration.SelectedConfigurationIndex;
                ConfigurationItem configurationItem = Model.Configuration.ConfigurationItems[selectedConfigurationIndex];

                Model.Configuration.ConfigurationItems.RemoveAt(selectedConfigurationIndex);
                Model.Configuration.ConfigurationItems.Insert(selectedConfigurationIndex + 1, configurationItem);

                Model.Configuration.SelectedConfigurationIndex = Model.Configuration.ConfigurationItems.IndexOf(configurationItem);
            }
        }

        private void ShowAbout()
        {
            new AboutBox(MainWindow.Icon, showLanguageSelection: false)
            {
                Owner = MainWindow,
                UpdateChecker = MainWindow.UpdateChecker
            }.ShowDialog();
        }

        #endregion
    }

    #endregion

    #region WpfExtensions class

    internal static class WpfExtensions
    {
        // https://stackoverflow.com/a/978352/4206279
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

    #endregion
}
