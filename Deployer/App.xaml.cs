#region Usings

using System.Windows;
using WindowsGuiLibrary;
using GuiLibraryInterfaces;
using Microsoft.Extensions.DependencyInjection;
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
                .BuildServiceProvider();

        private static ServiceProvider _serviceProvider;
    }
}
