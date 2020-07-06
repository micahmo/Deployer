using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Deployer
{
    /// <summary>
    /// Interaction logic for PathVariableEditor.xaml
    /// </summary>
    public partial class PathVariableEditor : Window
    {
        public PathVariableEditor()
        {
            InitializeComponent();
        }

        internal PathVariable DataModel => DataContext as PathVariable;

        #region Event handlers

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void PathValueEditor_ValueAdded(object sender, EventArgs e)
        {
            DataModel?.PossibleValues.Add(new PossiblePathVariable());
        }

        private void PathValueEditor_ValueRemoved(object sender, ValueRemovedEventArgs e)
        {
            DataModel?.PossibleValues.Remove(e.RemovedValue);
        }

        #endregion
    }
}
