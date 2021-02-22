#region Usings

using System;
using System.Windows;
using WindowsGuiLibrary;
using GuiLibraryInterfaces;
using Microsoft.Extensions.DependencyInjection;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using Utilities;

#endregion

namespace Deployer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal static ServiceProvider ServiceProvider => _serviceProvider ??=
            new ServiceCollection()
                .AddSingleton<INotify, MessageBoxNotify>()
                .AddSingleton<IFileBrowser, WpfFileBrowser>()
                .AddSingleton<IShellContextMenu, WindowsShellContextMenu>()
                .AddSingleton(LogManager.RegisterLogger(new SessionFileLogger()))
                .AddSingleton(new Notifier(cfg =>
                {
                    cfg.PositionProvider = new WindowPositionProvider(Current.MainWindow, Corner.BottomLeft, 10, 10);
                    cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(TimeSpan.FromSeconds(10), MaximumNotificationCount.FromCount(5));
                    cfg.DisplayOptions.Width = 300;
                    cfg.Dispatcher = Current.Dispatcher;
                }))
                .BuildServiceProvider();

        private static ServiceProvider _serviceProvider;

        internal static bool ImportInProgress { get; set; }
    }
}
