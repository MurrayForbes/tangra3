﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32.SafeHandles;

namespace Tangra
{
    //  http://www.csharp411.com/console-output-from-winforms-application/

    public class ConsoleHelper
    {
        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(UInt32 dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();

        //[DllImport("kernel32.dll")]
        //private static extern bool GetFileInformationByHandle(SafeFileHandle hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        //[DllImport("kernel32.dll")]
        //private static extern SafeFileHandle GetStdHandle(UInt32 nStdHandle);

        //[DllImport("kernel32.dll")]
        //private static extern bool SetStdHandle(UInt32 nStdHandle, SafeFileHandle hHandle);

        //[DllImport("kernel32.dll")]
        //private static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, SafeFileHandle hSourceHandle,
        //    IntPtr hTargetProcessHandle, out SafeFileHandle lpTargetHandle, UInt32 dwDesiredAccess, Boolean bInheritHandle, UInt32 dwOptions);

        private const UInt32 ATTACH_PARENT_PROCESS = 0xFFFFFFFF;
        private const UInt32 STD_OUTPUT_HANDLE = 0xFFFFFFF5;
        private const UInt32 STD_ERROR_HANDLE = 0xFFFFFFF4;
        private const UInt32 DUPLICATE_SAME_ACCESS = 2;

        //private struct BY_HANDLE_FILE_INFORMATION
        //{
        //    public UInt32 FileAttributes;
        //    public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
        //    public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
        //    public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
        //    public UInt32 VolumeSerialNumber;
        //    public UInt32 FileSizeHigh;
        //    public UInt32 FileSizeLow;
        //    public UInt32 NumberOfLinks;
        //    public UInt32 FileIndexHigh;
        //    public UInt32 FileIndexLow;
        //}

        internal static void InitConsoleHandles()
        {
            //SafeFileHandle hStdOut, hStdErr, hStdOutDup, hStdErrDup;
            //BY_HANDLE_FILE_INFORMATION bhfi;

            //hStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
            //hStdErr = GetStdHandle(STD_ERROR_HANDLE);

            //// Get current process handle
            //IntPtr hProcess = Process.GetCurrentProcess().Handle;

            //// Duplicate Stdout handle to save initial value
            //DuplicateHandle(hProcess, hStdOut, hProcess, out hStdOutDup, 0, true, DUPLICATE_SAME_ACCESS);

            //// Duplicate Stderr handle to save initial value
            //DuplicateHandle(hProcess, hStdErr, hProcess, out hStdErrDup, 0, true, DUPLICATE_SAME_ACCESS);

            // Attach to console window – this may modify the standard handles
            AttachConsole(ATTACH_PARENT_PROCESS);

            //// Adjust the standard handles
            //if (GetFileInformationByHandle(GetStdHandle(STD_OUTPUT_HANDLE), out bhfi))
            //{
            //    SetStdHandle(STD_OUTPUT_HANDLE, hStdOutDup);
            //}
            //else
            //{
            //    SetStdHandle(STD_OUTPUT_HANDLE, hStdOut);
            //}

            //if (GetFileInformationByHandle(GetStdHandle(STD_ERROR_HANDLE), out bhfi))
            //{
            //    SetStdHandle(STD_ERROR_HANDLE, hStdErrDup);
            //}
            //else
            //{
            //    SetStdHandle(STD_ERROR_HANDLE, hStdErr);
            //}
        }

        internal static void ReleaseConsoleHandles()
        {
            SendKeys.SendWait("{ENTER}");

            FreeConsole();
        }
    }
}
