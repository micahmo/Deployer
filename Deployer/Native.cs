﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Deployer
{
    public static class Native
    {
        #region Wrappers

        public static Icon ExtractAssociationIcon(string filePath)
        {
            return Icon.FromHandle(ExtractAssociatedIcon(IntPtr.Zero, filePath, out _));
        }

        public static bool DirectoryExists(string path)
        {
            return Utilities.FileSystem.IsUncPath(path, out Uri uri)
                ? ConnectToNetworkShare($@"\\{uri.Host}") && Directory.Exists(path)
                : Directory.Exists(path);
        }

        private static bool ConnectToNetworkShare(string path)
        {
            NETRESOURCE nr = new NETRESOURCE {dwType = ResourceType.DISK, lpRemoteName = path};
            
            int res = WNetUseConnection(IntPtr.Zero, nr, string.Empty, string.Empty, Connect.INTERACTIVE, null, null, null);

            return res == WINERROR.ERROR_SUCCESS || res == WINERROR.ERROR_SESSION_CREDENTIAL_CONFLICT;
        }

        /// <summary>
        /// Find out what process(es) have a lock on the specified file.
        /// </summary>
        /// <param name="path">Path of the file.</param>
        /// <returns>Processes locking the file</returns>
        /// <remarks>See also:
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa373661(v=vs.85).aspx
        /// http://wyupdate.googlecode.com/svn-history/r401/trunk/frmFilesInUse.cs
        /// https://stackoverflow.com/a/20623302/4206279
        /// </remarks>
        public static List<Process> GetLockingProcesses(string path)
        {
            uint handle;
            string key = Guid.NewGuid().ToString();
            List<Process> processes = new List<Process>();

            int res = RmStartSession(out handle, 0, key);
            if (res != 0) throw new Exception("Could not begin restart session.  Unable to determine file locker.");

            try
            {
                const int ERROR_MORE_DATA = 234;
                uint pnProcInfoNeeded = 0,
                    pnProcInfo = 0,
                    lpdwRebootReasons = RmRebootReasonNone;

                string[] resources = new string[] { path }; // Just checking on one resource.

                res = RmRegisterResources(handle, (uint)resources.Length, resources, 0, null, 0, null);

                if (res != 0) throw new Exception("Could not register resource.");

                //Note: there's a race condition here -- the first call to RmGetList() returns
                //      the total number of process. However, when we call RmGetList() again to get
                //      the actual processes this number may have increased.
                res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);

                if (res == ERROR_MORE_DATA)
                {
                    // Create an array to store the process results
                    RM_PROCESS_INFO[] processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
                    pnProcInfo = pnProcInfoNeeded;

                    // Get the list
                    res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);
                    if (res == 0)
                    {
                        processes = new List<Process>((int)pnProcInfo);

                        // Enumerate all of the results and add them to the 
                        // list to be returned
                        for (int i = 0; i < pnProcInfo; i++)
                        {
                            try
                            {
                                processes.Add(Process.GetProcessById(processInfo[i].Process.dwProcessId));
                            }
                            // catch the error -- in case the process is no longer running
                            catch (ArgumentException)
                            {
                            }
                        }
                    }
                    else throw new Exception("Could not list processes locking resource.");
                }
                else if (res != 0)
                    throw new Exception("Could not list processes locking resource. Failed to get size of result.");
            }
            finally
            {
                RmEndSession(handle);
            }

            return processes;
        }

        /// <summary>
        /// Given a <paramref name="process"/>, determines the file name of the main module (i.e., the path to the executable)
        /// </summary>
        /// <remarks>From StackOverflow: https://stackoverflow.com/a/48319879/4206279 </remarks>
        public static string GetMainModuleFileName(this Process process, int buffer = 1024)
        {
            var fileNameBuilder = new StringBuilder(buffer);
            uint bufferLength = (uint)fileNameBuilder.Capacity + 1;
            return QueryFullProcessImageName(process.Handle, 0, fileNameBuilder, ref bufferLength) ?
                fileNameBuilder.ToString() :
                null;
        }

        #endregion

        #region DllImports

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr ExtractAssociatedIcon(IntPtr hInst, string lpIconPath, out ushort lpiIcon);

        [DllImport("Mpr.dll")]
        private static extern int WNetUseConnection
        (
            IntPtr hwndOwner,
            NETRESOURCE lpNetResource,
            string lpPassword,
            string lpUserID,
            Connect dwFlags,
            string lpAccessName,
            string lpBufferSize,
            string lpResult
        );

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto)]
        private static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        private static extern int RmRegisterResources(uint pSessionHandle,
            UInt32 nFiles,
            string[] rgsFilenames,
            UInt32 nApplications,
            [In] RM_UNIQUE_PROCESS[] rgApplications,
            UInt32 nServices,
            string[] rgsServiceNames);

        [DllImport("rstrtmgr.dll")]
        private static extern int RmGetList(uint dwSessionHandle,
            out uint pnProcInfoNeeded,
            ref uint pnProcInfo,
            [In, Out] RM_PROCESS_INFO[] rgAffectedApps,
            ref uint lpdwRebootReasons);

        [DllImport("rstrtmgr.dll")]
        private static extern int RmEndSession(uint pSessionHandle);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] uint dwFlags, [Out] StringBuilder lpExeName, [In, Out] ref uint lpdwSize);

        #endregion

        #region Native classes and structs

        [StructLayout(LayoutKind.Sequential)]
        private class NETRESOURCE
        {
            public ResourceScope dwScope = 0;
            public ResourceType dwType = 0;
            public ResourceDisplayType dwDisplayType = 0;
            public ResourceUsage dwUsage = 0;

            public string lpLocalName = "";
            public string lpRemoteName = "";
            public string lpComment = "";
            public string lpProvider = "";
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RM_UNIQUE_PROCESS
        {
            public int dwProcessId;
            public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
            public string strAppName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
            public string strServiceShortName;

            public RM_APP_TYPE ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)] public bool bRestartable;
        }

        #endregion

        #region Native enums

        [Flags]
        private enum Connect
        {
            UPDATE_PROFILE = 0x00000001,
            INTERACTIVE = 0x00000008,
            PROMPT = 0x00000010,
            REDIRECT = 0x00000080,
            LOCALDRIVE = 0x00000100,
            COMMANDLINE = 0x00000800,
            CMD_SAVECRED = 0x00001000,
        }

        private enum ResourceDisplayType
        {
            GENERIC = 0x00000000,
            DOMAIN = 0x00000001,
            SERVER = 0x00000002,
            SHARE = 0x00000003,
            FILE = 0x00000004,
            GROUP = 0x00000005,
            NETWORK = 0x00000006,
            ROOT = 0x00000007,
            SHAREADMIN = 0x00000008,
            DIRECTORY = 0x00000009,
            TREE = 0x0000000A,
            NDSCONTAINER = 0x0000000A,
        }

        [Flags]
        private enum ResourceUsage
        {
            CONNECTABLE = 0x00000001,
            CONTAINER = 0x00000002,
            NOLOCALDEVICE = 0x00000004,
            SIBLING = 0x00000008,
            ATTACHED = 0x00000010,
        }
        private enum ResourceType
        {
            ANY = 0x00000000,
            DISK = 0x00000001,
            PRINT = 0x00000002,
        }

        private enum RM_APP_TYPE
        {
            RmUnknownApp = 0,
            RmMainWindow = 1,
            RmOtherWindow = 2,
            RmService = 3,
            RmExplorer = 4,
            RmConsole = 5,
            RmCritical = 1000
        }

        #endregion

        #region Native constants

        private class WINERROR
        {
            /// <summary>
            /// The operation completed successfully.
            /// </summary>
            public const int ERROR_SUCCESS = 0;

            /// <summary>
            /// Multiple connections to a server or shared resource by the same user, using more than one user name, are not allowed. Disconnect all previous connections to the server or shared resource and try again..
            /// </summary>
            public const int ERROR_SESSION_CREDENTIAL_CONFLICT = 1219;
        }

        private const int RmRebootReasonNone = 0;

        private const int CCH_RM_MAX_APP_NAME = 255;

        private const int CCH_RM_MAX_SVC_NAME = 63;

        #endregion
    }
}