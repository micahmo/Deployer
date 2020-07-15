using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;

namespace Deployer
{
    /// <summary>
    /// Interaction logic for PathVariablesDataGrid.xaml
    /// </summary>
    public partial class PathVariablesDataGrid : DataGrid
    {
        public PathVariablesDataGrid()
        {
            InitializeComponent();
        }

        internal MainWindowModel DataModel => DataContext as MainWindowModel;

        public PathVariablesModel ViewModel { get; } = new PathVariablesModel();

        private void _this_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource is UIElement control && e.Key == Key.Enter)
            {
                e.Handled = true;
                control.MoveFocus(new TraversalRequest(FocusNavigationDirection.Right));
            }
        }
    }

    public class PathVariablesModel
    {
        public PathVariablesModel()
        {
            Commands = new PathVariablesCommands();
        }
        
        public PathVariablesCommands Commands { get; }
    }

    public class PathVariablesCommands
    {
        #region ICommands

        public ICommand ChooseValuesCommand => _chooseValuesCommand ??= new RelayCommand<PathVariablesDataGrid>(ChooseValues);
        private RelayCommand<PathVariablesDataGrid> _chooseValuesCommand;

        #endregion

        #region Implementations

        private void ChooseValues(PathVariablesDataGrid control)
        {
            if (control.DataModel.SelectedPathVariable is { })
            {
                if (_openPathVariableEditors.TryGetValue(control.DataModel.SelectedPathVariable.Guid, out var pathVariableEditor) && pathVariableEditor.IsVisible)
                {
                    if (pathVariableEditor.WindowState == WindowState.Minimized)
                    {
                        pathVariableEditor.WindowState = WindowState.Normal;
                    }
                    pathVariableEditor.Activate();
                }
                else
                {
                    (_openPathVariableEditors[control.DataModel.SelectedPathVariable.Guid] =
                        new PathVariableEditor { DataContext = control.DataModel.SelectedPathVariable }).Show();
                }
            }
        }

        private static readonly Dictionary<Guid, PathVariableEditor> _openPathVariableEditors = new Dictionary<Guid, PathVariableEditor>();

        #endregion
    }
}
