using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace Common
{
    class AttachWindowCore
    {
        public event EventHandler<EventArgs> TargetLostDetached;

        public bool IsAttached { get; private set; } = false;

        private Window OwnerWindow;
        private IntPtr OwnerHwnd;

        public void Init(Window window)
        {
            OwnerWindow = window;
            OwnerHwnd = new WindowInteropHelper(window).Handle;

            HwndSource source = PresentationSource.FromVisual(window) as HwndSource;
            source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case Native.WM_DESTROY:
                    if (IsAttached)
                    {
                        handled = true;
                        Detach();
                        TargetLostDetached?.Invoke(this, new EventArgs());
                    }
                    break;
            }

            return IntPtr.Zero;
        }

        public void Detach()
        {
            IntPtr hwnd = OwnerHwnd;

            IntPtr desktopHwnd = Native.GetDesktopWindow();
            Native.SetParent(hwnd, desktopHwnd);

            IsAttached = false;
        }

        public bool AttachToClient()
        {
            IntPtr hwnd = OwnerHwnd;
            IntPtr clientHwnd = GetClientHwnd();

            if (clientHwnd != IntPtr.Zero)
            {
                Native.ShowWindow(clientHwnd, (int)Native.ShowWindowCommands.Normal);
                Native.SetForegroundWindow(clientHwnd);

                Native.SetParent(hwnd, clientHwnd);
                IsAttached = true;

                Console.WriteLine($"Attached to {clientHwnd.ToInt64():X}");
            }

            return IsAttached;
        }

        private IntPtr GetClientHwnd()
        {
            IntPtr clientHwnd = Native.FindWindow("RCLIENT", "League of Legends");
            return clientHwnd;
        }

        public Rect GetClientWindowRect()
        {
            IntPtr clientHwnd = GetClientHwnd();
            Native.GetWindowRect(clientHwnd, out Native.RECT lpRect);

            Rect rect = new Rect(lpRect.Left, lpRect.Top, lpRect.Right - lpRect.Left, lpRect.Bottom - lpRect.Top);
            return rect;
        }

        private IntPtr GetClientBrowserHwnd(IntPtr clientHwnd)
        {
            IntPtr browserHwnd = Native.FindWindowEx(clientHwnd, IntPtr.Zero, "CefBrowserWindow", string.Empty);
            return browserHwnd;
        }

        
    }
}
