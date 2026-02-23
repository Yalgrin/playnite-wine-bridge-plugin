using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace WineBridgePlugin.Utils
{
    public class WineFifo
    {
        // DesiredAccess
        private const uint GENERIC_WRITE = 0x40000000;

        // ShareMode
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint FILE_SHARE_DELETE = 0x00000004;

        // CreationDisposition
        private const uint OPEN_EXISTING = 3;

        // FlagsAndAttributes
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeFileHandle CreateFileW(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        public static FileStream OpenWriteStream(string winePathToFifo, int bufferSize = 4096)
        {
            // Note: Opening a FIFO for write will block until a reader is connected.
            var handle = CreateFileW(
                winePathToFifo,
                GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero);

            if (handle == null || handle.IsInvalid)
            {
                throw new FileNotFoundException($"Failed to open FIFO: {winePathToFifo}");
            }

            // This constructor avoids the "device not a file" path-based checks.
            return new FileStream(handle, FileAccess.Write, bufferSize, isAsync: false);
        }
    }
}