using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

namespace NFT.NavyReader
{
    class WindowHelper
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

        [DllImport("user32.dll")]
        static extern int SetForegroundWindow(IntPtr hwnd);
        private enum ShowWindowEnum
        {
            Hide = 0,
            ShowNormal = 1, ShowMinimized = 2, ShowMaximized = 3,
            Maximize = 3, ShowNormalNoActivate = 4, Show = 5,
            Minimize = 6, ShowMinNoActivate = 7, ShowNoActivate = 8,
            Restore = 9, ShowDefault = 10, ForceMinimized = 11
        };

        public static void BringMainWindowToFront(Process bProcess)
        {
            // check if the window is hidden / minimized
            if (bProcess.MainWindowHandle == IntPtr.Zero) ShowWindow(bProcess.Handle, ShowWindowEnum.Restore);
            // set user the focus to the window
            SetForegroundWindow(bProcess.MainWindowHandle);
        }


        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);
        public static Point GetWindowPosition(Process bProcess)
        {
            Rect rect = new Rect();
            GetWindowRect(bProcess.MainWindowHandle, ref rect);
            return new Point(rect.Left, rect.Top);
        }
        struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOZORDER = 0x0004;

        public static void SetWindowPosition(Process bProcess)
        {
            var result = SetWindowPos(bProcess.MainWindowHandle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOZORDER);            
        }


    }
}
