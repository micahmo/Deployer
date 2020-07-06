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
        public static Thread GetIcons(FileCollection fileCollection)
        {
            Thread thread = new Thread(ThreadProc);
            thread.Start(fileCollection);
            return thread;
        }

        private static void ThreadProc(object parameter)
        {
            if ((parameter as FileCollection)?.Files is { } files)
            {
                //foreach (FileItem fileItem in _filesToProcess.GetConsumingEnumerable())
                foreach (FileItem fileItem in files)
                {
                    if (fileItem.CancelLoad) continue;

                    string file = fileItem.FullName;
                    if (File.Exists(file))
                    {
                        string extension = Path.GetExtension(file);
                        if (!string.IsNullOrEmpty(extension))
                        {
                            if (extension != ".exe")
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

                        if (fileItem._icon is { })
                        {
                            Application.Current?.Dispatcher?.BeginInvoke(new Action(() => { fileItem.RaisePropertyChanged(nameof(FileItem.Icon)); }), DispatcherPriority.Background);
                        }
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
        //private static readonly BlockingCollection<FileItem> _filesToProcess = new BlockingCollection<FileItem>();
    }
}