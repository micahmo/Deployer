#region Usings

using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using Deployer.Properties;
using GuiLibraryInterfaces;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic.Devices;
using Utilities;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using SpecialFolder = System.Environment.SpecialFolder;

#endregion

namespace Deployer
{
    /// <summary>
    /// Defines the root configuration class for items in Deployer
    /// </summary>
    [Serializable]
    public class Configuration : ObservableObject
    {
        #region Singleton

        [XmlIgnore]
        public static Configuration Instance
        {
            get => _instance;
            private set
            {
                _instance = value;
                
                // This code is what forces the file lists to initially load upon launch.
                _instance.ConfigurationItems.ToList().ForEach(c =>
                {
                    c.SourceDirectories.ToList().ForEach(d => d.RaisePropertyChanged(nameof(d.Path)));
                    c.DestinationDirectories.ToList().ForEach(d => d.RaisePropertyChanged(nameof(d.Path)));
                });
            }
        }
        private static Configuration _instance;

        #endregion

        #region Data members (public properties)

        public ObservableCollection<ConfigurationItem> ConfigurationItems { get; } = new ObservableCollection<ConfigurationItem>();

        public double ConfigurationItemsWidth
        {
            get => _configurationItemsWidth;
            set => Set(nameof(ConfigurationItemsWidth), ref _configurationItemsWidth, value);
        }
        private double _configurationItemsWidth = 200;

        [XmlIgnore]
        public GridLength ConfigurationItemsHeight
        {
            get => new GridLength(ConfigurationItemsHeightValue, ConfigurationItemsHeightGridUnitType);
            set
            {
                _configurationItemsHeightValue = value.Value;
                _configurationItemsHeightGridUnitType = value.GridUnitType;
                RaisePropertyChanged(nameof(ConfigurationItemsHeight));
            }
        }

        public double ConfigurationItemsHeightValue
        {
            get => _configurationItemsHeightValue;
            set
            {
                _configurationItemsHeightValue = value;
                RaisePropertyChanged(nameof(ConfigurationItemsHeight));
            }
        }
        private double _configurationItemsHeightValue = 1;

        public GridUnitType ConfigurationItemsHeightGridUnitType
        {
            get => _configurationItemsHeightGridUnitType;
            set
            {
                _configurationItemsHeightGridUnitType = value;
                RaisePropertyChanged(nameof(ConfigurationItemsHeight));
            }
        }
        private GridUnitType _configurationItemsHeightGridUnitType = GridUnitType.Star;

        public int SelectedConfigurationIndex
        {
            get => _selectedConfigurationIndex;
            set => Set(nameof(SelectedConfigurationIndex), ref _selectedConfigurationIndex, value);
        }
        private int _selectedConfigurationIndex;

        public WindowState WindowState
        {
            get => _windowState;
            set
            {
                if (value != WindowState.Minimized)
                {
                    Set(nameof(WindowState), ref _windowState, value);
                }
            }
        }
        private WindowState _windowState;

        public Size WindowSize
        {
            get => _windowSize;
            set => Set(nameof(WindowSize), ref _windowSize, value);
        }
        private Size _windowSize = new Size(1200, 800);

        public Point WindowLocation
        {
            get => _windowLocation;
            set => Set(nameof(WindowLocation), ref _windowLocation, value);
        }
        private Point _windowLocation = new Point(100, 100);

        public ObservableCollection<PathVariable> PathVariables { get; } = new ObservableCollection<PathVariable>();

        public bool CloseDialogOnErrors
        {
            get => _closeDialogOnErrors;
            set => Set(nameof(CloseDialogOnErrors), ref _closeDialogOnErrors, value);
        }
        private bool _closeDialogOnErrors;

        #endregion

        #region Public methods

        /// <summary>
        /// Checks all of the <see cref="ConfigurationItems"/> for default-named items.
        /// Returns the value of the highest default name, or 0 if no items have the default name.
        /// </summary>
        /// <returns></returns>
        private int GetHighestDefaultConfigurationItem()
        {
            string regex = $"^{ConfigurationItem.DefaultName} (?'num'[0-9]*)$";

            int highestNumber = ConfigurationItems.Select(item =>
            {
                int.TryParse(Regex.Match(item.Name, regex).Groups["num"].Value, out int value);
                return value;
            }).Where(value => value > 0).OrderBy(num => num).LastOrDefault();


            return highestNumber;
        }

        /// <summary>
        /// Generates a default name for a new configuration item
        /// </summary>
        /// <returns></returns>
        public string GenerateNewConfigurationItemName()
        {
            return $"{ConfigurationItem.DefaultName} {GetHighestDefaultConfigurationItem() + 1}";
        }

        public string GenerateDuplicateConfigurationName(string existingName)
        {
            return string.Format(Resources.DuplicateConfigurationName, existingName);
        }

        public void ReloadCurrentConfiguration()
        {
            int selectedIndex = SelectedConfigurationIndex;
            SelectedConfigurationIndex = -1;
            SelectedConfigurationIndex = selectedIndex;

            if (ConfigurationItems.ElementAtOrDefault(selectedIndex) is { } selectedConfigurationItem)
            {
                selectedConfigurationItem.SourceDirectories.ToList().ForEach(d => d.RaisePropertyChanged(nameof(d.Path)));
                selectedConfigurationItem.DestinationDirectories.ToList().ForEach(d => d.RaisePropertyChanged(nameof(d.Path)));
            }
        }

        public void HardReloadCurrentConfiguration()
        {
            IconHelper.Reset();
            ReloadCurrentConfiguration();
        }

        public async Task<DeploymentItem> PrepareDeployment()
        {
            return await Task.Run(() =>
            {
                DeploymentItem result = null;

                if (ConfigurationItems.ElementAtOrDefault(SelectedConfigurationIndex) is { } configurationItem)
                {
                    List<FileCopyPair> filesToCopy = new List<FileCopyPair>();

                    foreach (DirectoryPair directoryPair in configurationItem.DirectoryPairs)
                    {
                        // Generate the list of files
                        filesToCopy.AddRange(
                            directoryPair.LeftFileCollection.GenerateFileCollection(fileViewOptions: FileViewOptions.ViewPendingFiles)
                            .Select(f => new FileCopyPair(f, directoryPair.Right.Path, f.IsDirectory)));
                    }

                    result = new DeploymentItem(configurationItem, filesToCopy);
                }

                return result;
            });
        }

        public async Task Deploy(DeploymentItem deploymentItem, CancellationTokenSource cancellationTokenSource = null,
            IProgress<DeployProgress> progress = null, IProgress<DeployError> errorProgress = null)
        {
            List<ServiceController> stoppedServices = new List<ServiceController>();
            List<string> killedProcesses = new List<string>();

            double i = 1;
            foreach (FileCopyPair fileCopyPair in deploymentItem.FilesToCopy)
            {
                try
                {
                    if (cancellationTokenSource?.IsCancellationRequested == true)
                    {
                        return;
                    }

                    string fileOrFolder = (fileCopyPair.SourceFile.FileInfo is FileInfo ? Resources.File : fileCopyPair.SourceFile.FileInfo is DirectoryInfo ? Resources.Folder : Resources.Item)
                                          .Transform(To.SentenceCase);
                    string sourceFolder = Path.GetDirectoryName(fileCopyPair.SourceFile.FullName);
                    string sourceFileFullName = fileCopyPair.SourceFile.FullName;
                    string destinationFileFullName = Path.Combine(fileCopyPair.DestinationPath, fileCopyPair.SourceFile.Name);
                    double percentComplete = (i / deploymentItem.FilesToCopy.Count) * 100;

                    progress?.Report(new DeployProgress(
                        Resources.CopyingFilesTitle, string.Join(Environment.NewLine,
                            string.Format(Resources.FromSource, sourceFolder),
                            string.Format(Resources.ToDestination, fileCopyPair.DestinationPath),
                            string.Format(Resources.AtoB, fileOrFolder, fileCopyPair.SourceFile.Name)),
                        percentComplete));

                    bool skipFile = false;
                    if (File.Exists(destinationFileFullName))
                    {
                        // Destination file exists and is on UNC path.
                        // Tell the user that we can't detect locking processes.
                        if (FileSystem.IsUncPath(destinationFileFullName, out _))
                        {
                            progress?.Report(new DeployProgress(
                                Resources.CopyingFilesTitle, string.Join(Environment.NewLine,
                                    string.Format(Resources.FromSource, sourceFolder),
                                    string.Format(Resources.ToDestination, fileCopyPair.DestinationPath),
                                    string.Format(Resources.AtoB, fileOrFolder, fileCopyPair.SourceFile.Name),
                                    string.Empty,
                                    Resources.UnableToDetectRemoteLockingProcesses),
                                percentComplete));
                        }

                        foreach (Process process in Native.GetLockingProcesses(destinationFileFullName))
                        {
                            if (deploymentItem.ConfigurationItem.LockedFileOptionSetting.Value == LockedFileOptions.Skip)
                            {
                                skipFile = true;
                                break;
                            }
                            else if (deploymentItem.ConfigurationItem.LockedFileOptionSetting.Value == LockedFileOptions.WaitForLockingProcesses)
                            {
                                if (process.HasExited == false)
                                {
                                    progress?.Report(new DeployProgress(Resources.StoppingLockingProcessesTitle, string.Join(Environment.NewLine,
                                        string.Format(Resources.FoundLockedFile, destinationFileFullName),
                                        string.Format(Resources.WaitingForLockingProcessToStop, process.ProcessName))));

                                    if (await WaitForProcessToExit(process, cancellationTokenSource) == false)
                                    {
                                        return;
                                    }
                                }
                            }
                            else if (deploymentItem.ConfigurationItem.LockedFileOptionSetting.Value == LockedFileOptions.StopLockingProcesses)
                            {
                                if (process.HasExited == false)
                                {
                                    // Check if it's a service
                                    if (await GetServiceFromProcess(process) is { } service)
                                    {
                                        progress?.Report(new DeployProgress(Resources.StoppingLockingProcessesTitle, string.Join(Environment.NewLine,
                                            string.Format(Resources.FoundLockedFile, destinationFileFullName),
                                            string.Format(Resources.StoppingLockingService, service.DisplayName))));

                                        // Shutdown the service and its dependencies
                                        var services = await StopServiceAndDependencies(service, process, deploymentItem.ConfigurationItem.StopServiceMethodSetting.Value,
                                            destinationFileFullName, cancellationTokenSource, progress, errorProgress);
                                        if (services is { })
                                        {
                                            stoppedServices.AddRange(services);
                                        }
                                        else
                                        {
                                            return;
                                        }
                                    }

                                    // Otherwise, it's a regular process
                                    else
                                    {
                                        progress?.Report(new DeployProgress(Resources.StoppingLockingProcessesTitle, string.Join(Environment.NewLine,
                                            string.Format(Resources.FoundLockedFile, destinationFileFullName),
                                            string.Format(Resources.KillingLockingProcess, process.ProcessName))));

                                        killedProcesses.Add(process.GetMainModuleFileName());
                                        process.Kill();
                                    }

                                    // No matter the process type or how we're killing it, wait for it to finally die before moving on.
                                    if (await WaitForProcessToExit(process, cancellationTokenSource) == false)
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                    }

                    if (skipFile)
                    {
                        continue;
                    }

                    if (fileCopyPair.SourceFile.IsDirectory == false && File.Exists(destinationFileFullName))
                    {
                        if (await WaitForFileToHaveAttributes(destinationFileFullName, FileAttributes.Normal, cancellationTokenSource) == false)
                        {
                            return;
                        }

                        if (await WaitForFileToBeDeleted(destinationFileFullName, cancellationTokenSource) == false)
                        {
                            return;
                        }
                    }
                    else if (fileCopyPair.SourceFile.IsDirectory && Native.DirectoryExists(destinationFileFullName))
                    {
                        if (await WaitForDirectoryToBeDeleted(destinationFileFullName, cancellationTokenSource) == false)
                        {
                            return;
                        }
                    }

                    await Task.Run(() =>
                    {
                        if (fileCopyPair.IsDirectory)
                        {
                            new Computer().FileSystem.CopyDirectory(sourceFileFullName, destinationFileFullName, true);
                        }
                        else
                        {
                            File.Copy(sourceFileFullName, destinationFileFullName, true);
                        }
                    });
                }
                catch (Exception ex)
                {
                    errorProgress?.Report(new DeployError(string.Join(Environment.NewLine,
                        string.Format(Resources.ErrorCopyingSourceToDestination, fileCopyPair.SourceFile.FullName, fileCopyPair.DestinationPath),
                        ex.ToString()), ex));
                }

                ++i;
            }

            // Restart all stopped services
            if (deploymentItem.ConfigurationItem.KilledProcessesSetting.Value)
            {
                foreach (ServiceController stoppedService in stoppedServices)
                {
                    stoppedService.Refresh();
                    if (stoppedService.Status == ServiceControllerStatus.Stopped)
                    {
                        progress?.Report(new DeployProgress(Resources.RestartingStoppedProcessesTitle, string.Format(Resources.RestartingStoppedService, stoppedService.DisplayName)));
                        try
                        {
                            stoppedService.Start();
                        }
                        catch (Exception ex)
                        {
                            errorProgress?.Report(new DeployError(string.Format(Resources.ErrorRestartingService, stoppedService.DisplayName), ex));
                        }
                    }
                }

                // Restart all killed processes
                foreach (string process in killedProcesses)
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = process,
                        WorkingDirectory = Path.GetDirectoryName(process)
                    };

                    progress?.Report(new DeployProgress(Resources.RestartingStoppedProcessesTitle, string.Format(Resources.RestartingKilledProcess, process)));

                    try
                    {
                        Process.Start(startInfo);
                    }
                    catch (Exception ex)
                    {
                        errorProgress?.Report(new DeployError(string.Format(Resources.ErrorRestartingProcess, process), ex));
                    }
                }
            }
        }

        #endregion

        #region Private methods

        private async Task<ServiceController> GetServiceFromProcess(Process process)
        {
            return await Task.Run(() =>
            {
                ServiceController processService = null;

                try
                {
                    processService = ServiceController.GetServices().Where(service =>
                    {
                        using (ManagementObject wmiService = new ManagementObject("Win32_Service.Name='" + service.ServiceName + "'"))
                        {
                            wmiService.Get();
                            string serviceFullPath = wmiService["PathName"].ToString().Replace("\"", "");

                            if (process.HasExited == false && serviceFullPath == process.GetMainModuleFileName())
                            {
                                return true;
                            }

                            return false;
                        }
                    }).FirstOrDefault();
                }
                catch
                {
                    // Don't let an exception kill us. Just return a null process.
                }

                return processService;
            });
        }

        private async Task<Process> GetProcessFromService(ServiceController service)
        {
            return await Task.Run(() =>
            {
                Process serviceProcess = null;

                try
                {
                    using ManagementObject wmiService = new ManagementObject("Win32_Service.Name='" + service.ServiceName + "'");
                    
                    if (wmiService.GetPropertyValue("ProcessId") is uint pid && Process.GetProcessById((int)pid) is { } process)
                    {
                        serviceProcess = process;
                    }
                }
                catch
                {
                    // Don't let an exception kill us. Just return a null process.
                }

                return serviceProcess;
            });
        }

        /// <summary>
        /// Gracefully stops the given service and all of its dependencies. Returns all stopped services.
        /// Returns null if the a cancelation is requested via the <paramref name="cancellationTokenSource"/>.
        /// </summary>
        private async Task<IEnumerable<ServiceController>> StopServiceAndDependencies(ServiceController serviceController, Process process, StopServiceMethods stopServiceMethod,
            string lockedFileName, CancellationTokenSource cancellationTokenSource = null, IProgress<DeployProgress> progress = null, IProgress<DeployError> errorProgress = null)
        {
            HashSet<ServiceController> allServices = new HashSet<ServiceController>();

            foreach (ServiceController dependency in serviceController.DependentServices)
            {
                var services = await StopServiceAndDependencies(dependency, await GetProcessFromService(dependency), stopServiceMethod, 
                    lockedFileName, cancellationTokenSource, progress, errorProgress);
                if (services is { })
                {
                    allServices.UnionWith(services);
                }
                else
                {
                    return null;
                }
            }

            serviceController.Refresh();
            if (serviceController.Status == ServiceControllerStatus.Running)
            {
                allServices.Add(serviceController);

                try
                {
                    progress?.Report(new DeployProgress(Resources.StoppingLockingProcessesTitle, string.Join(Environment.NewLine, 
                        string.Format(Resources.FoundLockedFile, lockedFileName),
                        string.Format(Resources.StoppingLockingService, serviceController.DisplayName))));

                    if (stopServiceMethod == StopServiceMethods.ShutdownGracefully || process is null)
                    {
                        serviceController.Stop();

                        if (await WaitForServiceToExit(serviceController, cancellationTokenSource) == false)
                        {
                            return null;
                        }
                    }
                    else if (stopServiceMethod == StopServiceMethods.Kill)
                    {
                        process.Kill();

                        if (await WaitForProcessToExit(process, cancellationTokenSource) == false)
                        {
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorProgress?.Report(new DeployError(string.Join(Environment.NewLine, 
                        string.Format(Resources.ErrorStoppingLockingService, serviceController.DisplayName),
                        ex.ToString()), ex));
                }
            }

            return allServices.Reverse();
        }

        /// <summary>
        /// Waits for the given process to exit gracefully.
        /// Returns true if the process has exited.
        /// Returns false if the <paramref name="cancellationTokenSource"/> requests a cancelation.
        /// </summary>
        /// <param name="process"></param>
        /// <param name="cancellationTokenSource"></param>
        /// <returns></returns>
        private async Task<bool> WaitForProcessToExit(Process process, CancellationTokenSource cancellationTokenSource)
        {
            return await Task.Run(async () =>
            {
                while (true)
                {
                    if (process.HasExited)
                    {
                        return true;
                    }
                    else if (cancellationTokenSource?.IsCancellationRequested == true)
                    {
                        return false;
                    }

                    await Task.Delay(10);
                }
            });
        }

        /// <summary>
        /// Waits for the given service to exit gracefully.
        /// Returns true if the service has exited.
        /// Returns false if the <paramref name="cancellationTokenSource"/> requests a cancelation.
        /// </summary>
        /// <param name="process"></param>
        /// <param name="cancellationTokenSource"></param>
        /// <returns></returns>
        private async Task<bool> WaitForServiceToExit(ServiceController service, CancellationTokenSource cancellationTokenSource = null)
        {
            return await Task.Run(async () =>
            {
                while (true)
                {
                    // IMPORTANT: Must refresh the service to get live status
                    service.Refresh();

                    if (service.Status == ServiceControllerStatus.Stopped)
                    {
                        return true;
                    }
                    else if (cancellationTokenSource?.IsCancellationRequested == true)
                    {
                        return false;
                    }

                    await Task.Delay(10);
                }
            });
        }

        private async Task<bool> WaitForFileToHaveAttributes(string fileName, FileAttributes targetAttributes, CancellationTokenSource cancellationTokenSource = null)
        {
            return await Task.Run(async () =>
            {
                File.SetAttributes(fileName, targetAttributes);

                while (true)
                {
                    if (File.GetAttributes(fileName) == targetAttributes)
                    {
                        return true;
                    }
                    else if (cancellationTokenSource?.IsCancellationRequested == true)
                    {
                        return false;
                    }

                    await Task.Delay(10);
                }
            });
        }

        private async Task<bool> WaitForFileToBeDeleted(string fileName, CancellationTokenSource cancellationTokenSource = null)
        {
            return await Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch
                    {
                        // Empty
                    }

                    if (File.Exists(fileName) == false)
                    {
                        return true;
                    }
                    else if (cancellationTokenSource?.IsCancellationRequested == true)
                    {
                        return false;
                    }

                    await Task.Delay(10);
                }
            });
        }

        private async Task<bool> WaitForDirectoryToBeDeleted(string path, CancellationTokenSource cancellationTokenSource = null)
        {
            return await Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        Directory.Delete(path, true);
                    }
                    catch
                    {
                        // Empty
                    }

                    if (Native.DirectoryExists(path) == false)
                    {
                        return true;
                    }
                    else if (cancellationTokenSource?.IsCancellationRequested == true)
                    {
                        return false;
                    }

                    Task.Delay(10);
                }
            });
        }

        #endregion

        #region Public static methods

        public static Configuration Load()
        {
            bool fileExistedBeforeFirstRead = File.Exists(XmlSerialization.GetCustomConfigFilePath(SpecialFolder.ApplicationData, CONFIG_FILE_NAME, createIfNotExists: false));

            try
            {
                return Instance = XmlSerialization.DeserializeObjectFromCustomConfigFile<Configuration>(CONFIG_FILE_NAME, SpecialFolder.ApplicationData);
            }
            catch
            {
                if (fileExistedBeforeFirstRead)
                {
                    // Timestamp the backup name so that there can be multiple
                    string backupName = $"{Path.GetFileNameWithoutExtension(CONFIG_FILE_NAME)}.{DateTime.Now.ToString(@"s").Replace(@":", @".")}.xml";

                    // The file exists, but there was a problem deserializing it.
                    // Be sure to back up the existing file before overwriting it with an empty instance.
                    File.Copy(XmlSerialization.GetCustomConfigFilePath(SpecialFolder.ApplicationData, CONFIG_FILE_NAME),
                              XmlSerialization.GetCustomConfigFilePath(SpecialFolder.ApplicationData, backupName), overwrite: true);

                    // Inform the user that there was an error loading the existing configuration.
                    App.ServiceProvider.GetRequiredService<INotify>().Warning(string.Join(Environment.NewLine,
                            Resources.ErrorLoadingExistingConfiguration,
                            string.Empty,
                            Resources.OldConfigurationAvailableAt,
                            XmlSerialization.GetCustomConfigFilePath(SpecialFolder.ApplicationData, backupName)),
                        Resources.Warning);
                }

                Save(new Configuration());
                return Load();
            }
        }

        public static void Save(Configuration configuration)
        {
            XmlSerialization.SerializeObjectToCustomConfigFile(CONFIG_FILE_NAME, configuration, SpecialFolder.ApplicationData);
        }

        public static ConfigurationItem GetConfigurationItem(DirectoryItem directoryItem)
        {
            return Instance.ConfigurationItems.FirstOrDefault(i => i.SourceDirectories.Contains(directoryItem) || i.DestinationDirectories.Contains(directoryItem));
        }

        private const string CONFIG_FILE_NAME = "DeployerConfig.xml";

        #endregion
    }

    #region Enums 

    public enum NonExistingFileOptions
    {
        [Display(Description = nameof(Resources.Skip), ResourceType = typeof(Resources))]
        Skip,

        [Display(Description = nameof(Resources.Copy), ResourceType = typeof(Resources))]
        Copy
    }

    public enum ExistingFileOptions
    {
        [Display(Description = nameof(Resources.Skip), ResourceType = typeof(Resources))]
        Skip,

        [Display(Description = nameof(Resources.Replace), ResourceType = typeof(Resources))]
        Replace
    }

    public enum LockedFileOptions
    {
        [Display(Description = nameof(Resources.AutomaticallyStopLockingProcesses), ResourceType = typeof(Resources))]
        StopLockingProcesses,
        
        [Display(Description = nameof(Resources.WaitForLockingProcesses), ResourceType = typeof(Resources))]
        WaitForLockingProcesses,

        [Display(Description = nameof(Resources.SkipLockedFiles), ResourceType = typeof(Resources))]
        Skip
    }

    public enum FileViewOptions
    {
        [Display(Description = nameof(Resources.ViewAllFiles), ResourceType = typeof(Resources))]
        ViewAllFiles,

        [Display(Description = nameof(Resources.ViewPendingFiles), ResourceType = typeof(Resources))]
        ViewPendingFiles,

        [Display(Description = nameof(Resources.ViewExcludedFiles), ResourceType = typeof(Resources))]
        ViewExcludedFiles
    }

    public enum StopServiceMethods
    {

        [Display(Description = nameof(Resources.GracefulShutdown), ResourceType = typeof(Resources))]
        ShutdownGracefully,

        [Display(Description = nameof(Resources.Kill), ResourceType = typeof(Resources))]
        Kill
    }

    #endregion
}
