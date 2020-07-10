using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Deployer
{
    public static class IconHelper
    {
        public static CancellationTokenSource GetIcons(FileCollection fileCollection)
        {
            Thread thread = new Thread(ThreadProc);
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            thread.Start(new IconHelperParameters {FileCollection = fileCollection, CancellationTokenSource = cancellationTokenSource});
            return cancellationTokenSource;
        }

        private static void ThreadProc(object parameter)
        {
            if (parameter is IconHelperParameters iconHelperParameters && iconHelperParameters.FileCollection?.Files?.ToList() is { } files)
            {
                foreach (FileItem fileItem in files)
                {
                    if (iconHelperParameters.CancellationTokenSource?.IsCancellationRequested == true) return;

                    try
                    {
                        if (fileItem.IsDirectory)
                        {
                            if (_iconCache.TryGetValue(DIRECTORY_ICON_KEY, out var cachedDirectoryIcon) && cachedDirectoryIcon is { })
                            {
                                fileItem._icon = cachedDirectoryIcon;
                            }
                            else
                            {
                                fileItem._icon = _iconCache[DIRECTORY_ICON_KEY] = IconToImageSource(Native.GetFolderIcon());
                                _iconCache[DIRECTORY_ICON_KEY]?.Freeze();
                            }
                        }
                        else
                        {
                            string file = fileItem.FullName;
                            if (File.Exists(file))
                            {
                                string extension = Path.GetExtension(file);
                                if (!string.IsNullOrEmpty(extension))
                                {
                                    if (_nonCachedExtensions.Contains(extension) == false)
                                    {
                                        if (_iconCache.TryGetValue(Path.GetExtension(file), out var cachedImage) && cachedImage is { })
                                        {
                                            fileItem._icon = cachedImage;
                                        }
                                        else
                                        {
                                            fileItem._icon = _iconCache[extension] = IconToImageSource(Native.ExtractAssociationIcon(file));

                                            // Must freeze to pass from background thread to main thread
                                            _iconCache[extension]?.Freeze();
                                        }
                                    }
                                    else
                                    {
                                        if (_iconCache.TryGetValue(file, out var cachedImage) && cachedImage is { })
                                        {
                                            fileItem._icon = cachedImage;
                                        }
                                        else
                                        {
                                            fileItem._icon = _iconCache[file] = IconToImageSource(Native.ExtractAssociationIcon(file));

                                            // Must freeze to pass from background thread to main thread
                                            _iconCache[file]?.Freeze();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Do not let icon load kill us.
                    }

                    if (fileItem._icon is { })
                    {
                        Application.Current?.Dispatcher?.BeginInvoke(new Action(() => { fileItem.RaisePropertyChanged(nameof(FileItem.Icon)); }), DispatcherPriority.Background);
                    }
                }
            }
        }

        private static ImageSource IconToImageSource(Icon icon)
        {
            ImageSource result = null;
            try
            {
                result = Imaging.CreateBitmapSourceFromHBitmap(icon.ToBitmap().GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            catch { }

            return result;
        }

        private static readonly ConcurrentDictionary<string, ImageSource> _iconCache = new ConcurrentDictionary<string, ImageSource>();

        private static readonly List<string> _nonCachedExtensions = new List<string> {".exe", ".ico", ".bmp"};

        private static string DIRECTORY_ICON_KEY = nameof(DIRECTORY_ICON_KEY);

        private class IconHelperParameters
        {
            public FileCollection FileCollection { get; set; }
            public CancellationTokenSource CancellationTokenSource { get; set; }
        }
    }
}