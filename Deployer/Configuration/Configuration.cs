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
using ToastNotifications;
using ToastNotifications.Core;
using ToastNotifications.Messages;
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

        public ObservableCollection<ConfigurationItem> ConfigurationItems { get; private set; } = new ObservableCollection<ConfigurationItem>();

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

        public ObservableCollection<PathVariable> PathVariables { get; private set; } = new ObservableCollection<PathVariable>();

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
            catch (Exception rootException)
            {
                if (fileExistedBeforeFirstRead)
                {
                    // Find the real exception
                    Exception innermostException = rootException;
                    while (innermostException.InnerException is { })
                    {
                        innermostException = innermostException.InnerException;
                    }

                    // Inform the user that there was an error loading the existing configuration.
                    NotifyOption copyDetails = new NotifyOption {Text = Resources.CopyErrorDetails};
                    NotifyOption backupAndContinue = new NotifyOption {Text = Resources.BackupAndContinue};
                    NotifyOption quit = new NotifyOption {Text = Resources.Quit};

                    NotifyOption result;
                    do
                    {
                        result = App.ServiceProvider.GetRequiredService<INotify>().Warning(string.Join(Environment.NewLine,
                                Resources.ErrorLoadingExistingConfiguration,
                                string.Empty,
                                innermostException.Message),
                            Resources.Warning, copyDetails, backupAndContinue, quit);

                        // How do they want to proceed
                        if (result == copyDetails)
                        {
                            Clipboard.SetText(rootException.ToString());
                        }
                        else if (result == quit)
                        {
                            Environment.Exit(1);
                        }
                        else if (result == backupAndContinue)
                        {
                            // Timestamp the backup name so that there can be multiple
                            string backupName = GetTimeStampedConfigFileName();

                            // Make the backup
                            File.Copy(XmlSerialization.GetCustomConfigFilePath(SpecialFolder.ApplicationData, CONFIG_FILE_NAME),
                                      XmlSerialization.GetCustomConfigFilePath(SpecialFolder.ApplicationData, backupName), overwrite: true);

                            // Notify the user where the backup is
                            NotifyOption openConfigLocation = new NotifyOption {Text = Resources.OpenConfigFileLocation};
                            NotifyOption cont = new NotifyOption {Text = Resources.Continue};

                            do
                            {
                                result = App.ServiceProvider.GetRequiredService<INotify>().Information(string.Join(Environment.NewLine,
                                        Resources.OldConfigurationAvailableAt,
                                        string.Empty,
                                        XmlSerialization.GetCustomConfigFilePath(SpecialFolder.ApplicationData, backupName),
                                        string.Empty,
                                        Resources.NewConfigCreated),
                                    Resources.BackupCreated, openConfigLocation, cont);

                                if (result == openConfigLocation)
                                {
                                    Process.Start(Path.GetDirectoryName(XmlSerialization.GetCustomConfigFilePath(SpecialFolder.ApplicationData, backupName)));
                                }
                                else if (result == cont)
                                {
                                    // Save an empty config and load it
                                    Save(new Configuration());
                                    return Load();
                                }
                            } while (result == openConfigLocation);
                        }
                    } while (result == copyDetails);
                }

                // If we haven't done anything else, save an empty config and load it
                Save(new Configuration());
                return Load();
            }
        }

        public static Configuration Load(string importedConfigurationFullPath, LoadMode loadMode)
        {
            // Always make a backup before importing
            string backupName = GetTimeStampedConfigFileName();

            // Make the backup
            File.Copy(XmlSerialization.GetCustomConfigFilePath(SpecialFolder.ApplicationData, CONFIG_FILE_NAME),
                      XmlSerialization.GetCustomConfigFilePath(SpecialFolder.ApplicationData, backupName), overwrite: true);

            // First, try to deserialize the imported configuration. If it fails, we'll stop
            Configuration importedConfiguration = XmlSerialization.DeserializeObjectFromCustomConfigFile<Configuration>(importedConfigurationFullPath);

            // Don't do any error checking on the imported configuration. If there is anything wrong (e.g., null), an exception will be thrown,
            // which will be caught at the higher level and properly displayed to the user.

            // If we get here, it deserialized correctly. Now try to apply it, depending on the mode.
            if (loadMode == LoadMode.Replace)
            {
                // Easy case, just replace the current instance.
                // But only replace ConfigurationItems and PathVariables. Everything else is a "global" setting.

                Instance.ConfigurationItems = importedConfiguration.ConfigurationItems;
                Instance.RaisePropertyChanged(nameof(Instance.ConfigurationItems));

                Instance.PathVariables = importedConfiguration.PathVariables;
                Instance.RaisePropertyChanged(nameof(Instance.PathVariables));

                // Also bring over the selected index
                Instance.SelectedConfigurationIndex = importedConfiguration.SelectedConfigurationIndex;
            }
            else if (loadMode == LoadMode.Append)
            {
                // Maintain the existing lists and add new items.
                // Since we can't AddRange to ObservableCollection, and we don't want an event for every item added,
                //  we'll copy the items here, append to a local list, and reassign a new ObservableCollection back.

                List<ConfigurationItem> existingConfigurationItems = Instance.ConfigurationItems.ToList();
                existingConfigurationItems.AddRange(importedConfiguration.ConfigurationItems);
                Instance.ConfigurationItems = new ObservableCollection<ConfigurationItem>(existingConfigurationItems);
                Instance.RaisePropertyChanged(nameof(Instance.ConfigurationItems));

                List<PathVariable> existingPathVariables = Instance.PathVariables.ToList();
                existingPathVariables.AddRange(importedConfiguration.PathVariables);
                Instance.PathVariables = new ObservableCollection<PathVariable>(existingPathVariables);
                Instance.RaisePropertyChanged(nameof(Instance.PathVariables));

                // In this case, no need to bring over the selected index, since the existing selected index is still valid.
            }

            return Instance;
        }

        public static void Save(Configuration configuration)
        {
            XmlSerialization.SerializeObjectToCustomConfigFile(CONFIG_FILE_NAME, configuration, SpecialFolder.ApplicationData);
        }

        public static void Save(Configuration configuration, string configFileFullPath)
        {
            XmlSerialization.SerializeObjectToCustomConfigFile(configFileFullPath, configuration);
        }

        public static ConfigurationItem GetConfigurationItem(DirectoryItem directoryItem)
        {
            return Instance.ConfigurationItems.FirstOrDefault(i => i.SourceDirectories.Contains(directoryItem) || i.DestinationDirectories.Contains(directoryItem));
        }

        public static string GetTimeStampedConfigFileName(string extension = null) => $"{Path.GetFileNameWithoutExtension(CONFIG_FILE_NAME)}.{DateTime.Now.ToString(@"s").Replace(@":", @".")}{extension ?? ".xml"}";

        public static string UserConfigFileFilter { get; } = "Deployer Config Files|*.dcfg";

        private const string CONFIG_FILE_NAME = "DeployerConfig.xml";

        #endregion
    }

    #region Enums

    /// <summary>
    /// Defines the types of modes that can be used when importing a configuration
    /// </summary>
    public enum LoadMode
    {
        Append,
        Replace
    }

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
