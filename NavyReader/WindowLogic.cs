using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Windows;

namespace NFT.NavyReader
{
    enum ShowWindowEnum
    {
        Hide = 0,
        ShowNormal = 1, ShowMinimized = 2, ShowMaximized = 3,
        Maximize = 3, ShowNormalNoActivate = 4, Show = 5,
        Minimize = 6, ShowMinNoActivate = 7, ShowNoActivate = 8,
        Restore = 9, ShowDefault = 10, ForceMinimized = 11
    };

    class WindowLogic
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

        [DllImport("user32.dll")]
        static extern int SetForegroundWindow(IntPtr hwnd);


        public static void WindowToFront(Process bProcess)
        {
            if (bProcess.MainWindowHandle == IntPtr.Zero) ShowWindow(bProcess.MainWindowHandle, ShowWindowEnum.Restore);
            SetForegroundWindow(bProcess.MainWindowHandle);
        }

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hwnd, ref Rectangle rectangle);
        public static Point GetWindowPosition(Process bProcess)
        {
            var rect = new Rectangle();
            GetWindowRect(bProcess.MainWindowHandle, ref rect);
            return rect.Location;
        }
        
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOZORDER = 0x0004;

        public static void SetWindowPosition(Process bProcess, int x, int y)
        {
            if (!SetWindowPos(bProcess.MainWindowHandle, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER)) 
                throw new Exception($"SetWindowPosition() error: code= {Marshal.GetLastWin32Error()}");
        }


        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetCursorPos(int x, int y);
        public static void SetCursor(int x, int y)
        {
            if (!SetCursorPos(x, y)) throw new Exception($"SetCursor() error: code= {Marshal.GetLastWin32Error()}");
        }

        static double GetWindowsScaling()
        {
            var w = Screen.PrimaryScreen.WorkingArea.Width;
            var w2 = Screen.PrimaryScreen.Bounds.Width;
            var w3 = SystemParameters.PrimaryScreenWidth;
            return w2 / w3;
        }

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        public enum DeviceCap
        {
            VERTRES = 10,
            DESKTOPVERTRES = 117,
            // http://pinvoke.net/default.aspx/gdi32/GetDeviceCaps.html

            LOGPIXELSX = 88,
            LOGPIXELSY = 90
        }
        public static double GetScalingFactor()
        {
            using var g = Graphics.FromHwnd(IntPtr.Zero);
            var hDc = IntPtr.Zero;
            try
            {
                hDc = g.GetHdc();
                var a = GetDeviceCaps(hDc, (int)DeviceCap.LOGPIXELSY);
                int logical = GetDeviceCaps(hDc, (int)DeviceCap.VERTRES);
                int physical = GetDeviceCaps(hDc, (int)DeviceCap.DESKTOPVERTRES);

                return physical / (double)logical;
            }
            finally
            {
                if(hDc != IntPtr.Zero) g?.ReleaseHdc(hDc);
            }
        }
    }
}
