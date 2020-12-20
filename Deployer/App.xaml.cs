#region Usings

using System.Windows;
using WindowsGuiLibrary;
using GuiLibraryInterfaces;
using HarmonyLib;
using Utilities;

#endregion

namespace Deployer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // Automatically perform patches based on HarmonyPatch attribute
            new Harmony("mdm.patch").PatchAll();
        }
    }

    public static class Dependencies
    {
        // TODO: Do some better version of DI
        public static INotify Notify { get; } = new MessageBoxNotify();
        public static IFileBrowser FileBrowser { get; } = new WpfFileBrowser();
        public static IShellContextMenu ShellContextMenu { get; } = new WindowsShellContextMenu();
        public static SessionFileLogger SessionFileLogger { get; } = LogManager.RegisterLogger(new SessionFileLogger());
    }
}
