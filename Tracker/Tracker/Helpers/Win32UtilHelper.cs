using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Tracker.Extensions;

namespace Tracker.Helpers
{
    public static class Win32UtilHelper
    {
        internal static class NativeMethods
        {
            #region Structures

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            internal class MemoryStatusEx
            {
                public uint dwLength;
                public uint dwMemoryLoad;
                public ulong ullTotalPhys;
                public ulong ullAvailPhys;
                public ulong ullTotalPageFile;
                public ulong ullAvailPageFile;
                public ulong ullTotalVirtual;
                public ulong ullAvailVirtual;
                public ulong ullAvailExtendedVirtual;

                public MemoryStatusEx()
                {
                    dwLength = (uint)Marshal.SizeOf(this);
                }
            }

            #endregion

            #region Imports

            #region Kernel32

            [DllImport("Kernel32.dll")]
            internal static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] uint dwFlags, [Out] StringBuilder lpExeName, [In, Out] ref uint lpdwSize);

            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx lpBuffer);

            [DllImport("kernel32.dll")]
            public static extern uint SetThreadExecutionState(uint esFlags);

            #endregion

            #region SHCore

            [DllImport("Shcore.dll")]
            internal static extern IntPtr GetDpiForMonitor([In] IntPtr hmonitor, [In] DpiType dpiType, [Out] out uint dpiX, [Out] out uint dpiY);

            #endregion

            #region User32

            [DllImport("user32.dll")]
            internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
            // Unregisters the hot key with Windows.
            [DllImport("user32.dll")]
            internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

            [DllImport("user32.dll")]
            internal static extern IntPtr GetActiveWindow();

            [DllImport("User32.dll", SetLastError = true, EntryPoint = "SendMessage")]
            internal static extern IntPtr SendMessage(IntPtr hWnd, uint msg, int wParam, ref CopyDataStruct lParam);

            [DllImport("User32.dll", SetLastError = true, EntryPoint = "SendMessage")]
            internal static extern IntPtr SendMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

            [DllImport("user32.dll")]
            internal static extern bool SetWindowPos(
                int hWnd, // handle to window
                int hWndInsertAfter, // placement-order handle
                short x, // horizontal position
                short y, // vertical position
                short cx, // width
                short cy, // height
                uint uFlags); // window-positioning options

            [DllImport("user32.dll")]
            internal static extern int FindWindow(
                string lpClassName, // class name
                string lpWindowName); // window name

            [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
            internal static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetCursorPos(out Point lpPoint);

            [DllImport("user32.dll")]
            internal static extern bool IsWindowVisible(IntPtr hWnd);

            [DllImport("user32.dll")]
            internal static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

            [DllImport("user32.dll")]
            internal static extern int SetForegroundWindow(IntPtr hWnd);

            [DllImport("User32.dll")]
            internal static extern IntPtr MonitorFromPoint([In] System.Windows.Point pt, [In] uint dwFlags);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetWindowPlacement(
                IntPtr hWnd, ref WindowPlacement lpwndpl);

            [DllImport("user32.dll")]
            internal static extern IntPtr GetDC(IntPtr hwnd);

            [DllImport("user32.dll")]
            internal static extern IntPtr GetDesktopWindow();

            [DllImport("user32.dll")]
            internal static extern IntPtr GetWindowDC(IntPtr hWnd);

            [DllImport("user32.dll")]
            internal static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);

            internal const int WsVisible = 0x10000000;
            internal const int WsExToolwindow = 0x00000080;

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern IntPtr GetWindow(IntPtr hWnd, int uCmd);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool IsIconic(IntPtr hWnd);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetClientRect(IntPtr hWnd, ref Rect lpRect);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

            public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern int GetWindowTextLength(IntPtr hWnd);

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

            #endregion

            #region GDI32

            [DllImport("gdi32.dll")]
            internal static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

            [DllImport("gdi32.dll")]
            internal static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);

            [DllImport("gdi32.dll")]
            internal static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
                int nWidth, int nHeight, IntPtr hObjectSource,
                int nXSrc, int nYSrc, int dwRop);

            [DllImport("gdi32.dll")]
            internal static extern IntPtr CreateCompatibleBitmap(IntPtr hDc, int nWidth,
                int nHeight);

            [DllImport("gdi32.dll")]
            internal static extern IntPtr CreateCompatibleDC(IntPtr hDc);

            [DllImport("gdi32.dll")]
            internal static extern bool DeleteDC(IntPtr hDc);

            [DllImport("gdi32.dll")]
            internal static extern bool DeleteObject(IntPtr hObject);

            [DllImport("gdi32.dll")]
            internal static extern IntPtr SelectObject(IntPtr hDc, IntPtr hObject);

            [DllImport("gdi32.dll")]
            internal static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

            [DllImport("gdi32.dll")]
            internal static extern int GetPixelFormat(IntPtr hdc);

            #endregion

            #region Shell32

            [DllImport("shell32.dll", CharSet = CharSet.Auto)]
            public static extern int SHGetKnownFolderPath(ref Guid id, int flags, IntPtr token, out IntPtr path);

            #endregion

            #region DWMApi

            [DllImport("dwmapi.dll")]
            internal static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr thumb);

            [DllImport("dwmapi.dll")]
            internal static extern int DwmUnregisterThumbnail(IntPtr thumb);

            [DllImport("dwmapi.dll")]
            internal static extern int DwmQueryThumbnailSourceSize(IntPtr thumb, out Size size);

            [DllImport("dwmapi.dll")]
            internal static extern int DwmUpdateThumbnailProperties(IntPtr hThumb, ref ThumbnailProperties props);

            [DllImport("dwmapi.dll")]
            internal static extern int DwmGetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, out bool pvAttribute, int cbAttribute);

            #endregion

            #endregion
        }

        #region Constants

        public const int SrcCopy = 0x00CC0020; // BitBlt dwRop parameter
        public const uint WmCopyData = 0x4A;
        public const uint WmSysCommand = 0x0112;
        public const uint WmActivate = 0x0006;
        public const uint WmNcActivate = 0x0086;
        public const int ScRestore = 0xF120;
        public const int WmHotkey = 0x0312;
        public const int WmActivateApp = 0x1C;

        public const int WmUser = 0x0400;
        public const int WmMeetingEventMsg = WmUser + 1;
        public const int WmSharingEventMsg = WmUser + 2;

        // flags for SetThreadExecutionState
        public const uint EsAwayModeRequired = 0x00000040;
        public const uint EsContinuous = 0x80000000;
        public const uint EsDisplayRequired = 0x00000002;
        public const uint EsSystemRequired = 0x00000001;

        #endregion

        #region Enums

        public enum DwmWindowAttribute : uint
        {
            DwmACloaked = 14
        }

        public enum TernaryRasterOperations
        {
            SrcCopy = 0x00CC0020,
            CaptureBlt = 0x40000000
        }

        public enum ShowWindowCommands
        {
            Hide = 0,
            Normal = 1,
            Minimized = 2,
            Maximized = 3,
        }

        public enum DpiType
        {
            Effective = 0,
            Angular = 1,
            Raw = 2,
        }

        #endregion

        #region Structures

        [StructLayout(LayoutKind.Sequential)]
        public struct Margins
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ThumbnailProperties
        {
            public uint dwFlags;
            public Rect rcDestination;
            public Rect rcSource;
            public byte opacity;
            public bool fVisible;
            public bool fSourceClientAreaOnly;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Size
        {
            public int cx;
            public int cy;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CopyDataStruct
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public int X;
            public int Y;

            public Point(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public Rect(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowPlacement
        {
            public int length;
            public int flags;
            public ShowWindowCommands showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }

        // DWM thumbnail constants
        public const int DwmTnpVisible = 0x00000008;
        public const int DwmTnpSourceClientAreaOnly = 0x00000001;

        // DWM thumbnail structure
        [StructLayout(LayoutKind.Sequential)]
        public struct DwmThumbnailProperties
        {
            public int dwFlags;
            public Rect rcDestination;
            public Rect rcSource;
            public byte opacity;
            public bool fVisible;
            public bool fSourceClientAreaOnly;
        }

        #endregion

        public static MainWindow? GetMainWindow()
        {
            return Application.Current.Windows.OfType<MainWindow>().Select(window => window).FirstOrDefault();
        }

        /// <summary>
        /// Returns a List of <see cref="Window"/> of a specific type
        /// </summary>
        /// <typeparam name="T"></typeparam> 
        /// <returns></returns>
        public static List<Window> GetWindowsByType<T>()
        {
            var windows = new List<Window>();

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (CheckIfWindowExistsByType<T>())
                {
                    foreach (var window in Application.Current.Windows.OfType<T>())
                    {
                        if (window is Window windowType)
                        {
                            windows.Add(windowType);
                        }
                    }
                }
            });

            return windows;
        }

        public static bool CheckIfWindowExistsByType<T>()
        {
            return Application.Current.Dispatcher.BackgroundInvoke(() =>
            {
                var window = Application.Current.Windows.OfType<T>().Select(window => window).FirstOrDefault();
                return window != null;
            });
        }

        public static bool GetGlobalMemoryStats(out uint memoryLoad, out ulong totalMem, out ulong availMem)
        {
            memoryLoad = 0;
            totalMem = 0;
            availMem = 0;

            var memStatus = new NativeMethods.MemoryStatusEx();
            if (NativeMethods.GlobalMemoryStatusEx(memStatus))
            {
                totalMem = memStatus.ullTotalPhys;
                availMem = memStatus.ullAvailPhys;
                memoryLoad = memStatus.dwMemoryLoad;

                return true;
            }

            return false;
        }
    }
}
