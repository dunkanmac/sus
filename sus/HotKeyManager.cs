using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace SUS
{
    public static class HotKeyManager
    {
        delegate void RegisterHotKeyDelegate(IntPtr hwnd, int id, KeyModifiers modifiers, Keys key);
        delegate void UnRegisterHotKeyDelegate(IntPtr hwnd, int id);

        private static int _id = 0;
        private static volatile MessageWindow _wnd;
        private static volatile IntPtr _hwnd;
        private static ManualResetEvent _windowReadyEvent = new ManualResetEvent(false);

        public static event EventHandler<HotKeyEventArgs> HotKeyPressed;

        static HotKeyManager()
        {
            Thread messageLoop = new Thread(() =>
            {
                Application.Run(new MessageWindow());
            });
            messageLoop.Name = "MessageLoopThread";
            messageLoop.IsBackground = true;
            messageLoop.Start();
        }

        public static int RegisterHotKey(Keys key, KeyModifiers modifiers)
        {
            _windowReadyEvent.WaitOne();
            int id = Interlocked.Increment(ref _id);
            _wnd.Invoke(new RegisterHotKeyDelegate(RegisterHotKeyInternal), _hwnd, id, modifiers, key);
            return id;
        }

        public static void UnregisterHotKey(int id)
        {
            _wnd.Invoke(new UnRegisterHotKeyDelegate(UnRegisterHotKeyInternal), _hwnd, id);
        }

        private static void RegisterHotKeyInternal(IntPtr hwnd, int id, KeyModifiers modifiers, Keys key)
        {
            if (!SuManager.RegisterHotKey(hwnd, id, (uint)modifiers, key))
            {
                if (Marshal.GetLastWin32Error() == SuManager.ERROR_HOTKEY_ALREADY_REGISTERED)
                    throw new Exception("Hotkey allready registered");
                else
                    throw new Win32Exception();
            }
        }

        private static void UnRegisterHotKeyInternal(IntPtr hwnd, int id)
        {
            if (!SuManager.UnregisterHotKey(_hwnd, id))
            {
                if (Marshal.GetLastWin32Error() == SuManager.ERROR_HOTKEY_ALREADY_REGISTERED)
                    throw new Exception("Hotkey allready registered");
                else
                    throw new Win32Exception();
            }
        }

        private static void OnHotKeyPressed(HotKeyEventArgs e)
        {
            HotKeyPressed?.Invoke(null, e);
        }

        private class MessageWindow : Form
        {
            public MessageWindow()
            {
                _wnd = this;
                _hwnd = this.Handle;
                _windowReadyEvent.Set();
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == SuManager.WM_HOTKEY)
                {
                    HotKeyEventArgs e = new HotKeyEventArgs(m.LParam);
                    HotKeyManager.OnHotKeyPressed(e);
                }

                base.WndProc(ref m);
            }

            protected override void SetVisibleCore(bool value)
            {
                base.SetVisibleCore(false);
            }
        }
    }

    public class HotKeyEventArgs : EventArgs
    {
        public readonly Keys Key;
        public readonly KeyModifiers Modifiers;

        public HotKeyEventArgs(Keys key, KeyModifiers modifiers)
        {
            this.Key = key;
            this.Modifiers = modifiers;
        }

        public HotKeyEventArgs(IntPtr hotKeyParam)
        {
            uint param = (uint)hotKeyParam.ToInt64();
            Key = (Keys)((param & 0xffff0000) >> 16);
            Modifiers = (KeyModifiers)(param & 0x0000ffff);
        }
    }

    [Flags]
    public enum KeyModifiers
    {
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8,
        NoRepeat = 0x4000
    }
}
