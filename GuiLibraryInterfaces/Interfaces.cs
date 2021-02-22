#region Usings

using System.IO;

#endregion

namespace GuiLibraryInterfaces
{
    #region INotify interface

    public interface INotify
    {
        QuestionResult Question(string message, string title, QuestionOptions options);

        NotifyOption Question(string message, string title, NotifyOption firstOption, NotifyOption secondOption, NotifyOption thirdOption);

        void Information(string message, string title);

        NotifyOption Information(string message, string title, NotifyOption firstOption, NotifyOption secondOption);

        void Warning(string message, string title);

        NotifyOption Warning(string message, string title, NotifyOption firstOption, NotifyOption secondOption, NotifyOption thirdOption);
    }

    #region QuestionOptions enum

    public enum QuestionOptions
    {
        YesNo,
    }

    #endregion

    #region QuestionResult enum

    public enum QuestionResult
    {
        Yes,
        No,
        OK,
        Cancel,
        None
    }

    #endregion

    #region NotifyOption class

    public class NotifyOption
    {
        public string Text { get; set; }

        public string Description { get; set; }
    }

    #endregion

    #endregion

    #region IFileBrowser interface

    public interface IFileBrowser
    {
        string BrowseForDirectory(string initialDirectory = null);

        bool SaveFile(string initialDirectory, string initialFileName, out string savedFileName);

        bool OpenFile(string initialDirectory, string filter, out string openedFileName);
    }

    #endregion

    #region IShellContextMenu interface

    public interface IShellContextMenu
    {
        void Show(FileSystemInfo[] fileInfo, Point point);
    }

    #region Point struct

    public struct Point
    {
        public Point(double x, double y) => (X, Y) = (x, y);
        public double X { get; }
        public double Y { get; }
    }

    #endregion

    #endregion
}
