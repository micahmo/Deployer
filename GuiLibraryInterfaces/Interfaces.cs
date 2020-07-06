﻿#region Usings

using System.IO;

#endregion

namespace GuiLibraryInterfaces
{
    public interface INotify
    {
        QuestionResult Question(string message, string title, QuestionOptions options);
    }

    public enum QuestionOptions
    {
        YesNo,
    }

    public enum QuestionResult
    {
        Yes,
        No,
        OK,
        Cancel,
        None
    }

    public interface IFileBrowser
    {
        string BrowseForDirectory(string initialDirectory = null);
    }

    public interface IShellContextMenu
    {
        void Show(FileInfo[] fileInfo, Point point);
    }

    public struct Point
    {
        public Point(double x, double y) => (X, Y) = (x, y);
        public double X { get; }
        public double Y { get; }
    }
}