#region Usings

using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms.VisualStyles;
using System.Xml.Serialization;
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
                if (_instance.PathVariables.Any())
                {
                    _instance.ConfigurationItems.ToList().ForEach(c =>
                    {
                        c.SourceDirectories.ToList().ForEach(d => d.RaisePropertyChanged(nameof(d.Path)));
                        c.DestinationDirectories.ToList().ForEach(d => d.RaisePropertyChanged(nameof(d.Path)));
                    });
                }
            }
        }
        private static Configuration _instance;

        #endregion

        #region Data members (public properties)

        public ObservableCollection<ConfigurationItem> ConfigurationItems { get; } = new ObservableCollection<ConfigurationItem>();

        public double ConfigurationItemsWidth
        {
            get => _configurationItemsWidth;
            set => Set(nameof(Configuration), ref _configurationItemsWidth, value);
        }
        private double _configurationItemsWidth = 200;

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
            return $"Copy of {existingName}";
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
                            .Select(f => new FileCopyPair(f, directoryPair.Right.Path)));
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
                if (cancellationTokenSource?.IsCancellationRequested == true)
                {
                    return;
                }

                string sourceFolder = Path.GetDirectoryName(fileCopyPair.SourceFile.FullName);
                string sourceFileFullName = fileCopyPair.SourceFile.FullName;
                string destinationFileFullName = Path.Combine(fileCopyPair.DestinationPath, fileCopyPair.SourceFile.Name);

                progress?.Report(new DeployProgress(
                    "Copying Files", $"From: {sourceFolder}{Environment.NewLine}To: {fileCopyPair.DestinationPath}{Environment.NewLine}File: {fileCopyPair.SourceFile.Name}",
                    (i / deploymentItem.FilesToCopy.Count) * 100));

                bool skipFile = false;
                if (File.Exists(destinationFileFullName))
                {
                    foreach (Process process in Native.GetLockingProcesses(destinationFileFullName))
                    {
                        if (deploymentItem.ConfigurationItem.LockedFileOptionSetting.Value == LockedFileOptions.Skip)
                        {
                            skipFile = true;
                            break;
                        }
                        else if (deploymentItem.ConfigurationItem.LockedFileOptionSetting.Value == LockedFileOptions.WaitForLockingProcesses)
                        {
                            progress?.Report(new DeployProgress("Locking Processes and Services", $"Found locked file: {destinationFileFullName}{Environment.NewLine}" +
                                                                                                  $"Waiting for locking process '{process.ProcessName}' to stop..."));

                            if (await WaitForProcessToExit(process, cancellationTokenSource) == false)
                            {
                                return;
                            }
                        }
                        else if (deploymentItem.ConfigurationItem.LockedFileOptionSetting.Value == LockedFileOptions.StopLockingProcesses)
                        {
                            if (process.HasExited == false)
                            {
                                // Check if it's a service
                                if (await GetServiceFromProcess(process) is { } service)
                                {
                                    progress?.Report(new DeployProgress("Stopping Locking Processes and Services", $"Found locked file: {destinationFileFullName}{Environment.NewLine}" +
                                                                                                                   $"Stopping locking service '{service.DisplayName}'..."));

                                    var services = await StopServiceAndDependencies(service, destinationFileFullName, cancellationTokenSource, progress, errorProgress);
                                    if (services is { })
                                    {
                                        stoppedServices.AddRange(services);
                                    }
                                    else
                                    {
                                        return;
                                    }

                                    if (await WaitForProcessToExit(process, cancellationTokenSource) == false)
                                    {
                                        return;
                                    }
                                }

                                // Otherwise, it's a regular process
                                else
                                {
                                    progress?.Report(new DeployProgress("Stopping Locking Processes and Services", $"Found locked file: {destinationFileFullName}{Environment.NewLine}" +
                                                                                                                   $"Killing locking process '{process.ProcessName}'..."));

                                    killedProcesses.Add(process.GetMainModuleFileName());
                                    process.Kill();

                                    if (await WaitForProcessToExit(process, cancellationTokenSource) == false)
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }

                if (skipFile)
                {
                    continue;
                }

                try
                {
                    if (File.Exists(destinationFileFullName))
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

                    File.Copy(sourceFileFullName, destinationFileFullName, true);
                }
                catch (Exception ex)
                {
                    errorProgress?.Report(new DeployError($"Error copying {fileCopyPair.SourceFile.FullName} to {fileCopyPair.DestinationPath}{Environment.NewLine}{ex}", ex));
                }

                ++i;
            }

            // Restart all stopped services
            foreach (ServiceController stoppedService in stoppedServices)
            {
                stoppedService.Refresh();
                if (stoppedService.Status == ServiceControllerStatus.Stopped)
                {
                    progress?.Report(new DeployProgress("Restarting Stopped Processes and Services", $"Restarting stopped service '{stoppedService.DisplayName}'..."));
                    try
                    {
                        stoppedService.Start();
                    }
                    catch (Exception ex)
                    {
                        errorProgress?.Report(new DeployError($"Error restarting service '{stoppedService.DisplayName}'", ex));
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

                progress?.Report(new DeployProgress("Restarting Stopped Processes and Services", $"Restarting killed process '{process}'..."));

                try
                {
                    Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    errorProgress?.Report(new DeployError($"Error restarting process '{process}'", ex));
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

        /// <summary>
        /// Gracefully stops the given service and all of its dependencies. Returns all stopped services.
        /// Returns null if the a cancelation is requested via the <paramref name="cancellationTokenSource"/>.
        /// </summary>
        private async Task<IEnumerable<ServiceController>> StopServiceAndDependencies(ServiceController serviceController, string lockedFileName,
            CancellationTokenSource cancellationTokenSource = null, IProgress<DeployProgress> progress = null, IProgress<DeployError> errorProgress = null)
        {
            HashSet<ServiceController> allServices = new HashSet<ServiceController>();

            foreach (ServiceController dependency in serviceController.DependentServices)
            {
                var services = await StopServiceAndDependencies(dependency, lockedFileName, cancellationTokenSource, progress, errorProgress);
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
                    progress?.Report(new DeployProgress("Stopping Locking Processes and Services", $"Found locked file: {lockedFileName}{Environment.NewLine}" +
                                                                                                   $"Stopping locking service '{serviceController.DisplayName}'..."));

                    serviceController.Stop();

                    if (await WaitForServiceToExit(serviceController, cancellationTokenSource) == false)
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    errorProgress?.Report(new DeployError($"Error stopping locking service {serviceController.DisplayName}{Environment.NewLine}{ex}", ex));
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

        #endregion

        #region Public static methods

        public static Configuration Load()
        {
            try
            {
                return Instance = XmlSerialization.DeserializeObjectFromCustomConfigFile<Configuration>(CONFIG_FILE_NAME, SpecialFolder.ApplicationData);
            }
            catch
            {
                if (File.Exists(XmlSerialization.GetCustomConfigFilePath(SpecialFolder.ApplicationData, CONFIG_FILE_NAME)))
                {
                    // The file exists, but there was a problem deserializing it.
                    // Be sure to back up the existing file before overwriting it with an empty instance.
                    File.Copy(XmlSerialization.GetCustomConfigFilePath(SpecialFolder.ApplicationData, CONFIG_FILE_NAME),
                              XmlSerialization.GetCustomConfigFilePath(SpecialFolder.ApplicationData, CONFIG_BACKUP_NAME), overwrite: true);
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
        private const string CONFIG_BACKUP_NAME = "DeployerConfig.bak.xml";

        #endregion
    }

    #region Enums 

    public enum NonExistingFileOptions
    {
        Skip,
        Copy
    }

    public enum ExistingFileOptions
    {
        Skip,
        Replace
    }

    public enum LockedFileOptions
    {
        [EnumDescription("Automatically stop and restart locking processes")]
        StopLockingProcesses,
        
        [EnumDescription("Wait for locking processes to stop")]
        WaitForLockingProcesses,

        [EnumDescription("Skipped locked files")]
        Skip
    }

    public enum FileViewOptions
    {
        [EnumDescription("View all files")]
        ViewAllFiles,

        [EnumDescription("View files which will be copied")]
        ViewPendingFiles
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class EnumDescriptionAttribute : Attribute
    {
        public EnumDescriptionAttribute(string description) => Description = description;
        public string Description { get; }
    }

    public class EnumDescriptionConverter : IValueConverter
    {
        #region IValueConverter members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object result = value?.ToString();

            FieldInfo fieldInfo = value?.GetType().GetField(value.ToString());
            if (fieldInfo?.GetCustomAttribute(typeof(EnumDescriptionAttribute)) is EnumDescriptionAttribute attribute)
            {
                result = attribute.Description;
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    #endregion
}
