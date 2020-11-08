using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NFT.NavyReader
{
    public sealed class KeyboardHotkey : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private class Window : NativeWindow, IDisposable
        {
            private const int WM_HOTKEY = 0x0312;
            public Window() => CreateHandle(new CreateParams());
            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);

                // check if we got a hot key pressed.
                if (m.Msg == WM_HOTKEY)
                {
                    // get the keys.
                    Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
                    _ModifierKeys modifier = (_ModifierKeys)((int)m.LParam & 0xFFFF);

                    // invoke the event to notify the parent.
                    KeyPressed?.Invoke(this, new KeyPressedEventArgs(modifier, key));
                }
            }
            public event EventHandler<KeyPressedEventArgs> KeyPressed;
            public void Dispose() => DestroyHandle();
        }

        private Window _window = new Window();
        private int _currentId;
        public KeyboardHotkey()
        {
            // register the event of the inner native window.
            _window.KeyPressed += delegate (object sender, KeyPressedEventArgs args)
            {
                KeyPressed?.Invoke(this, args);
            };
        }

        public void RegisterHotKey(_ModifierKeys modifier, Keys key)
        {
            _currentId = _currentId + 1;
            if (!RegisterHotKey(_window.Handle, _currentId, (uint)modifier, (uint)key)) 
                throw new InvalidOperationException("Couldn’t register the hot key.");
        }

        public event EventHandler<KeyPressedEventArgs> KeyPressed;

        #region IDisposable Members

        public void Dispose()
        {
            // unregister all the registered hot keys.
            for (int i = _currentId; i > 0; i--) UnregisterHotKey(_window.Handle, i);

            // dispose the inner native window.
            _window.Dispose();
        }

        #endregion
    }

    public class KeyPressedEventArgs : EventArgs
    {
        internal KeyPressedEventArgs(_ModifierKeys modifier, Keys key)
        {
            Modifier = modifier;
            Key = key;
        }

        public _ModifierKeys Modifier { get; }
        public Keys Key { get; }
    }

    [Flags]
    public enum _ModifierKeys : uint
    {
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8
    }

}
