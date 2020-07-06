#region Usings

using GuiLibraryInterfaces;
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

        #endregion
    }
}
