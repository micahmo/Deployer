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

        private void _this_LostFocus(object sender, RoutedEventArgs e)
        {
            UnselectAll();
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
            new PathVariableEditor {DataContext = control.DataModel.SelectedPathVariable}.ShowDialog();
        }

        #endregion
    }
}
