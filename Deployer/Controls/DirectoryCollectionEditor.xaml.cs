#region Usings

using GalaSoft.MvvmLight;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

#endregion

namespace Deployer
{
    /// <summary>
    /// Interaction logic for DirectoryCollectionEditor.xaml
    /// </summary>
    public partial class DirectoryCollectionEditor : UserControl
    {
        #region Constructor

        public DirectoryCollectionEditor()
        {
            InitializeComponent();
        }

        private DirectoryCollection DataModel => DataContext as DirectoryCollection;

        #endregion

        #region Event handlers

        private void DirectoryEditor_DirectoryRemoved(object sender, DirectoryRemovedEventArgs e)
        {
            DataModel?.Remove(e.RemovedDirectory);
        }

        private void DirectoryEditor_DirectoryAdded(object sender, EventArgs e)
        {
            DataModel?.Add(new DirectoryItem());
        }

        #endregion
    }
}
