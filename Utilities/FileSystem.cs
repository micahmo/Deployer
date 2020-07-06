#region Usings

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;

#endregion

namespace Utilities
{
    public static class FileSystem
    {
        /// <summary>
        /// Registers and returns a new <see cref="FileSystemWatcher"/> for the given <paramref name="path"/>.
        /// Returns <see langword="null"/> if <paramref name="path"/> does not exist.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static FileSystemWatcher RegisterFileSystemWatcher(string path)
        {
            FileSystemWatcher fileSystemWatcher = null;
            path = NormalizePath(path);

            if (_fileSystemWatchers.Keys.FirstOrDefault(f => f.Path == path) is { } existingFileSystemWatcher)
            {
                fileSystemWatcher = existingFileSystemWatcher;
            }
            else
            {
                fileSystemWatcher = new FileSystemWatcher(path)
                {
                    EnableRaisingEvents = true,
                    NotifyFilter = NotifyFilters.FileName
                                   | NotifyFilters.DirectoryName
                                   | NotifyFilters.Attributes
                                   | NotifyFilters.Size
                                   | NotifyFilters.LastWrite
                                   | NotifyFilters.LastAccess
                                   | NotifyFilters.CreationTime
                                   | NotifyFilters.Security
                };

                _fileSystemWatchers[fileSystemWatcher] = new List<FileSystemWatcherEventHandlers>();
            }

            return fileSystemWatcher;
        }

        /// <summary>
        /// Disables and disposes the given <paramref name="fileSystemWatcher"/> if it was previously registered via <see cref="RegisterFileSystemWatcher(string)"/>.
        /// Does nothing for a null <paramref name="fileSystemWatcher"/>.
        /// </summary>
        /// <param name="fileSystemWatcher"></param>
        public static void UnregisterFileSystemWatcher(FileSystemWatcher fileSystemWatcher)
        {
            if (fileSystemWatcher is { } && _fileSystemWatchers.ContainsKey(fileSystemWatcher))
            {
                UnregisterFileSystemWatcherEvents(fileSystemWatcher);
                fileSystemWatcher.EnableRaisingEvents = false;
                fileSystemWatcher.Dispose();
                _fileSystemWatchers.Remove(fileSystemWatcher);
            }
        }

        public static void RegisterFileSystemWatcherEvents(FileSystemWatcher fileSystemWatcher, FileSystemEventHandler eventHandler)
        {
            if (fileSystemWatcher is { } && _fileSystemWatchers.ContainsKey(fileSystemWatcher))
            {
                fileSystemWatcher.Changed += eventHandler;
                fileSystemWatcher.Created += eventHandler;
                fileSystemWatcher.Deleted += eventHandler;
                fileSystemWatcher.Renamed += new RenamedEventHandler(eventHandler);

                FileSystemWatcherEventHandlers newEventHandlers = new FileSystemWatcherEventHandlers(eventHandler, eventHandler, eventHandler, eventHandler);
                _fileSystemWatchers[fileSystemWatcher].Add(newEventHandlers);
            }
        }

        private static void UnregisterFileSystemWatcherEvents(FileSystemWatcher fileSystemWatcher)
        {
            if (fileSystemWatcher is { } && _fileSystemWatchers.ContainsKey(fileSystemWatcher))
            {
                _fileSystemWatchers[fileSystemWatcher].ForEach(events =>
                {
                    fileSystemWatcher.Changed -= events.ChangedHandler;
                    fileSystemWatcher.Created -= events.CreatedHandler;
                    fileSystemWatcher.Deleted -= events.DeletedHandler;
                    fileSystemWatcher.Renamed -= new RenamedEventHandler(events.RenamedHandler);
                });
            }
        }

        private static readonly Dictionary<FileSystemWatcher, List<FileSystemWatcherEventHandlers>> _fileSystemWatchers = new Dictionary<FileSystemWatcher, List<FileSystemWatcherEventHandlers>>();

        private class FileSystemWatcherEventHandlers
        {
            public FileSystemWatcherEventHandlers
                (FileSystemEventHandler changedHandler, FileSystemEventHandler createdHandler, FileSystemEventHandler deletedHandler, FileSystemEventHandler renamedHandler) =>
                (ChangedHandler, CreatedHandler, DeletedHandler, RenamedHandler) = (changedHandler, createdHandler, deletedHandler, renamedHandler);

            public FileSystemEventHandler ChangedHandler { get; }
            public FileSystemEventHandler CreatedHandler { get; }
            public FileSystemEventHandler DeletedHandler { get; }
            public FileSystemEventHandler RenamedHandler { get; }
        }

        public static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path)
                ? path
                : Path.GetFullPath(/*new Uri(*/path/*).LocalPath*/).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToLowerInvariant();
        }

        public static string NormalizeFileName(string path)
        {
            return Path.GetFileName(path)?.ToLowerInvariant();
        }

        // From https://stackoverflow.com/a/520796/4206279
        public static bool IsUncPath(string path, out Uri uri)
        {
            bool isUnc = Uri.TryCreate(path, UriKind.Absolute, out Uri uriOut) && uriOut.IsUnc;
            uri = uriOut;
            return isUnc;
        }
    }
}
