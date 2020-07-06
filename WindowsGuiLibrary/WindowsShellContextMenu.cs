#region Usings

using GuiLibraryInterfaces;
using System.IO;
using Peter;

#endregion

namespace WindowsGuiLibrary
{
    public class WindowsShellContextMenu : IShellContextMenu
    {
        #region IShellContextMenu members

        public void Show(FileInfo[] fileInfo, Point point)
        {
            ShellContextMenu scm = new ShellContextMenu();
            scm.ShowContextMenu(fileInfo, new System.Drawing.Point((int)point.X, (int)point.Y));
        }

        #endregion
    }
}
