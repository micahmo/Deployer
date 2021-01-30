#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Serialization;
using Deployer.Properties;
using GalaSoft.MvvmLight.Command;

#endregion

namespace Deployer
{
    /// <summary>
    /// Interaction logic for ConfigurationDataGrid.xaml
    /// </summary>
    public partial class ConfigurationDataGrid : DataGrid
    {
        public ConfigurationDataGrid()
        {
            InitializeComponent();
        }

        public ConfigurationDataGridModel ViewModel { get; } = new ConfigurationDataGridModel();

        internal MainWindowModel DataModel => DataContext as MainWindowModel;

        #region Event handlers

        private void _this_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource is UIElement control && e.Key == Key.Enter)
            {
                e.Handled = true;
                control.MoveFocus(new TraversalRequest(FocusNavigationDirection.Right));
            }
        }

        private void PreviousConfigurationCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataModel.Configuration.SelectedConfigurationIndex = Math.Max(DataModel.Configuration.SelectedConfigurationIndex - 1, 0);
        }

        private void NextConfigurationCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataModel.Configuration.SelectedConfigurationIndex = Math.Min(DataModel.Configuration.SelectedConfigurationIndex + 1, DataModel.Configuration.ConfigurationItems.Count - 1);
        }

        private void MoveConfigurationUpCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataModel.Commands.MoveConfigurationItemUpCommand?.Execute(null);
        }

        private void MoveConfigurationDownCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataModel.Commands.MoveConfigurationItemDownCommand?.Execute(null);
        }

        #endregion
    }

    public class ConfigurationDataGridModel
    {
        public ConfigurationDataGridModel()
        {
            Commands = new ConfigurationDataGridCommands();
        }

        public string ReloadButtonTooltip => string.Format(Resources.RefreshTooltip, ShortcutCommands.GetShortcutKey(ShortcutCommands.ReloadCurrentConfigurationCommand).FirstOrDefault());

        public ConfigurationDataGridCommands Commands { get; }
    }

    public class ConfigurationDataGridCommands
    {
        #region Commands

        public ICommand ReloadConfigurationItemCommand => _reloadConfigurationItemCommand ??= new RelayCommand(ReloadConfigurationItem);
        private RelayCommand _reloadConfigurationItemCommand;

        public ICommand DuplicateConfigurationItemCommand => _duplicateConfigurationItemCommand ??= new RelayCommand<ConfigurationDataGrid>(DuplicateConfigurationItem);
        private RelayCommand<ConfigurationDataGrid> _duplicateConfigurationItemCommand;

        public ICommand OpenConfigurationItemSettingsCommand => _openConfigurationItemSettingsCommand ??= new RelayCommand<ConfigurationDataGrid>(OpenConfigurationItemSettings);
        private RelayCommand<ConfigurationDataGrid> _openConfigurationItemSettingsCommand;

        #endregion

        #region Implementations

        private void ReloadConfigurationItem()
        {
            // Note: This assumes that the item must be selected before refreshing.
            // Then again, that is the only scenario where refreshing makes sense,
            // since changing the selection causes a refresh anyway.
            Configuration.Instance.ReloadCurrentConfiguration();
        }

        private void DuplicateConfigurationItem(ConfigurationDataGrid control)
        {
            if (control.DataModel.SelectedConfigurationItem is { })
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ConfigurationItem));
                using StringWriter stringWriter = new StringWriter();
                serializer.Serialize(stringWriter, control.DataModel.SelectedConfigurationItem);

                using StringReader stringReader = new StringReader(stringWriter.ToString());
                ConfigurationItem duplicatedConfigurationItem = (ConfigurationItem)serializer.Deserialize(stringReader);

                duplicatedConfigurationItem.Name = control.DataModel.Configuration.GenerateDuplicateConfigurationName(duplicatedConfigurationItem.Name);
                control.DataModel.Configuration.ConfigurationItems.Insert(control.DataModel.Configuration.SelectedConfigurationIndex + 1, duplicatedConfigurationItem);
                control.DataModel.Configuration.SelectedConfigurationIndex++;
            }
        }

        private void OpenConfigurationItemSettings(ConfigurationDataGrid control)
        {
            if (control.DataModel.SelectedConfigurationItem is { })
            {
                if (_openConfigurationItemSettings.TryGetValue(control.DataModel.SelectedConfigurationItem.GuidInfo.Guid, out var configurationItemSettings) && configurationItemSettings.IsVisible)
                {
                    if (configurationItemSettings.WindowState == WindowState.Minimized)
                    {
                        configurationItemSettings.WindowState = WindowState.Normal;
                    }
                    configurationItemSettings.Activate();
                }
                else
                {
                    (_openConfigurationItemSettings[control.DataModel.SelectedConfigurationItem.GuidInfo.Guid] =
                        new ConfigurationItemSettings {DataContext = control.DataModel.SelectedConfigurationItem}).Show();
                }
            }
        }

        private static readonly Dictionary<Guid, ConfigurationItemSettings> _openConfigurationItemSettings = new Dictionary<Guid, ConfigurationItemSettings>();

        #endregion
    }
}
