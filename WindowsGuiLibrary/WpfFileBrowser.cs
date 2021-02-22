#region Usings

using GuiLibraryInterfaces;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

#endregion

namespace WindowsGuiLibrary
{
    public class WpfFileBrowser : IFileBrowser
    {
        #region IFileBrowser implementation

        /// <inheritdoc/>
        public string BrowseForDirectory(string initialDirectory = null)
        {
            var folderBrowserDialog = new VistaFolderBrowserDialog {SelectedPath = initialDirectory};
            if (folderBrowserDialog.ShowDialog() == true)
            {
                return folderBrowserDialog.SelectedPath;
            }

            return null;
        }

        public bool SaveFile(string initialDirectory, string initialFileName, out string savedFileName)
        {
            savedFileName = default;

            var saveFileDialog = new SaveFileDialog {InitialDirectory = initialDirectory, FileName = initialFileName};
            bool res = saveFileDialog.ShowDialog() ?? false;
            
            if (res)
            {
                savedFileName = saveFileDialog.FileName;
            }

            return res;
        }

        public bool OpenFile(string initialDirectory, string filter, out string openedFileName)
        {
            openedFileName = default;

            var openFileDialog = new OpenFileDialog {InitialDirectory = initialDirectory, Multiselect = false, Filter = filter};
            bool res = openFileDialog.ShowDialog() ?? false;

            if (res)
            {
                openedFileName = openFileDialog.FileName;
            }

            return res;
        }

        #endregion
    }
}
