using System;
using System.Diagnostics;
using Bluegrams.Application;
using HarmonyLib;

namespace Deployer
{
    [HarmonyPatch(typeof(UpdateCheckerBase), nameof(UpdateCheckerBase.ShowUpdateDownload))]
    public class MyUpdateChecker
    {
        // Must be static for Harmony patching.
        // When using annotated patches, the replacement method name must be identifiable as one of the patch types (in this case, "Prefix")
        // Based on the class annotation, we know that this method is patching "ShowUpdateDownload" in "UpdateCheckerBase".
        // Also, parameter name must match exactly.
        public static bool Prefix(string file)
        {
            // Instead of showing the file in explorer (as the original method does),
            // we want to kill the current app, copy the file to the original location (with a backup of the original file)
            // and start it.

            string currentApplicationPath = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath;

            string[] commands =
            {
                // Kill the current process
                $"taskkill /f /pid {Process.GetCurrentProcess().Id}",

                // Wait for the process to die before we can rename the exe
                $"timeout 1",

                // Rename the current exe
                $"move /y \"{currentApplicationPath}\" \"{currentApplicationPath}.old\"",

                // Move the download to the current folder
                $"move /y \"{file}\" \"{currentApplicationPath}\"",

                // Launch the new exe
                $"\"{currentApplicationPath}\"",
            };

            new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Verb = "runas", // For elevated privileges
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    Arguments = "/C " + string.Join(" & ", commands)
                }
            }.Start();


            // Return false to prevent execution of the original method
            return false;
        }
    }
}
