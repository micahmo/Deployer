#region Usings

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

#endregion

namespace Deployer
{
    /// <summary>
    /// Interaction logic for FilesDataGrid.xaml
    /// </summary>
    public partial class FilesDataGrid : DataGrid
    {
        #region Constructor

        public FilesDataGrid()
        {
            InitializeComponent();
            Items.SortDescriptions.Add(new SortDescription(nameof(FileItem.Name), ListSortDirection.Ascending));
        }

        #endregion

        #region Overrides

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            // TODO: Perhaps allow user-configurable sorts

            // Have to reapply the SortDirection every time the ItemsSource changes
            Items.SortDescriptions.Add(new SortDescription(nameof(FileItem.Name), ListSortDirection.Ascending));

            // Have to reapply the sort directly to the column, otherwise the arrow disappears
            if (Columns.FirstOrDefault(c => c.SortMemberPath == nameof(FileItem.Name)) is { } column)
            {
                column.SortDirection = ListSortDirection.Ascending;
            }
        }

        #endregion

        #region Event handlers

        private void DataGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItems.OfType<FileItem>().Any())
            {
                Point mousePosition = Mouse.GetPosition(Window.GetWindow(this));
                Dependencies.ShellContextMenu.Show(dataGrid.SelectedItems.OfType<FileItem>().Select(f => f.FileInfo).ToArray(), new GuiLibraryInterfaces.Point(mousePosition.X, mousePosition.Y));
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is FileItem fileItem)
            {
                Process.Start(new ProcessStartInfo { FileName = fileItem.FullName, UseShellExecute = true });
            }
        }

        private void DataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollChanged?.Invoke(this, e);
        }

        #endregion

        #region Public events

        public event EventHandler<ScrollChangedEventArgs> ScrollChanged;

        #endregion
    }
}