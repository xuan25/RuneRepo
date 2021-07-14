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

            uint dwExStyle = Native.GetWindowLong(hwnd, (int)Native.WindowLongFlags.GWL_STYLE);
            dwExStyle &= ~(uint)Native.WindowStyles.WS_CHILDWINDOW;
            dwExStyle |= (uint)Native.WindowStyles.WS_CAPTION;
            Native.SetWindowLong(hwnd, (int)Native.WindowLongFlags.GWL_STYLE, dwExStyle);

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

                Native.GetWindowRect(clientHwnd, out Native.RECT lpRect);
                PositionOffset = new Point(lpRect.Left, lpRect.Top);

                uint dwExStyle = Native.GetWindowLong(hwnd, (int)Native.WindowLongFlags.GWL_STYLE);
                dwExStyle |= (uint)Native.WindowStyles.WS_CHILDWINDOW;
                dwExStyle &= ~(uint)Native.WindowStyles.WS_CAPTION;
                Native.SetWindowLong(hwnd, (int)Native.WindowLongFlags.GWL_STYLE, dwExStyle);

                Native.SetParent(hwnd, clientHwnd);
                IsAttached = true;

                Console.WriteLine($"Attached to {clientHwnd.ToInt64():X}");
            }

            return IsAttached;
        }

        public Point PositionOffset { get; private set; } = new Point(0, 0);

        public Rect GetClientWindowRect()
        {
            IntPtr clientHwnd = GetClientHwnd();
            Native.GetWindowRect(clientHwnd, out Native.RECT lpRect);

            Rect rect = new Rect(lpRect.Left, lpRect.Top, lpRect.Right - lpRect.Left, lpRect.Bottom - lpRect.Top);
            return rect;
        }

        private IntPtr GetClientHwnd()
        {
            IntPtr clientHwnd = Native.FindWindow("RCLIENT", "League of Legends");
            return clientHwnd;
        }

        private IntPtr GetClientBrowserHwnd(IntPtr clientHwnd)
        {
            IntPtr browserHwnd = Native.FindWindowEx(clientHwnd, IntPtr.Zero, "CefBrowserWindow", string.Empty);
            return browserHwnd;
        }

        

        public class Native
        {
            public const int WM_DESTROY = 0x2;

            [DllImport("user32.dll", EntryPoint = "FindWindow")]
            public extern static IntPtr FindWindow(string lpClassName, string lpWindowName);

            [Flags]
            public enum SendMessageTimeoutFlags : uint
            {
                SMTO_NORMAL = 0x0,
                SMTO_BLOCK = 0x1,
                SMTO_ABORTIFHUNG = 0x2,
                SMTO_NOTIMEOUTIFNOTHUNG = 0x8,
                SMTO_ERRORONEXIT = 0x20
            }

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern IntPtr SendMessageTimeout(
                IntPtr hWnd,
                uint Msg,
                IntPtr wParam,
                IntPtr lParam,
                SendMessageTimeoutFlags fuFlags,
                uint uTimeout,
                out IntPtr lpdwResult);

            public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int Left, Top, Right, Bottom;

                public RECT(int left, int top, int right, int bottom)
                {
                    Left = left;
                    Top = top;
                    Right = right;
                    Bottom = bottom;
                }
            }

            [DllImport("user32", ExactSpelling = true, SetLastError = true)]
            public static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, [In, Out] ref RECT rect, [MarshalAs(UnmanagedType.U4)] int cPoints);

            [Flags()]
            public enum DeviceContextValues : uint
            {
                /// <summary>DCX_WINDOW: Returns a DC that corresponds to the window rectangle rather
                /// than the client rectangle.</summary>
                Window = 0x00000001,
                /// <summary>DCX_CACHE: Returns a DC from the cache, rather than the OWNDC or CLASSDC
                /// window. Essentially overrides CS_OWNDC and CS_CLASSDC.</summary>
                Cache = 0x00000002,
                /// <summary>DCX_NORESETATTRS: Does not reset the attributes of this DC to the
                /// default attributes when this DC is released.</summary>
                NoResetAttrs = 0x00000004,
                /// <summary>DCX_CLIPCHILDREN: Excludes the visible regions of all child windows
                /// below the window identified by hWnd.</summary>
                ClipChildren = 0x00000008,
                /// <summary>DCX_CLIPSIBLINGS: Excludes the visible regions of all sibling windows
                /// above the window identified by hWnd.</summary>
                ClipSiblings = 0x00000010,
                /// <summary>DCX_PARENTCLIP: Uses the visible region of the parent window. The
                /// parent's WS_CLIPCHILDREN and CS_PARENTDC style bits are ignored. The origin is
                /// set to the upper-left corner of the window identified by hWnd.</summary>
                ParentClip = 0x00000020,
                /// <summary>DCX_EXCLUDERGN: The clipping region identified by hrgnClip is excluded
                /// from the visible region of the returned DC.</summary>
                ExcludeRgn = 0x00000040,
                /// <summary>DCX_INTERSECTRGN: The clipping region identified by hrgnClip is
                /// intersected with the visible region of the returned DC.</summary>
                IntersectRgn = 0x00000080,
                /// <summary>DCX_EXCLUDEUPDATE: Unknown...Undocumented</summary>
                ExcludeUpdate = 0x00000100,
                /// <summary>DCX_INTERSECTUPDATE: Unknown...Undocumented</summary>
                IntersectUpdate = 0x00000200,
                /// <summary>DCX_LOCKWINDOWUPDATE: Allows drawing even if there is a LockWindowUpdate
                /// call in effect that would otherwise exclude this window. Used for drawing during
                /// tracking.</summary>
                LockWindowUpdate = 0x00000400,
                /// <summary>DCX_VALIDATE When specified with DCX_INTERSECTUPDATE, causes the DC to
                /// be completely validated. Using this function with both DCX_INTERSECTUPDATE and
                /// DCX_VALIDATE is identical to using the BeginPaint function.</summary>
                Validate = 0x00200000,
            }

            [DllImport("user32.dll")]
            public static extern IntPtr GetDCEx(IntPtr hWnd, IntPtr hrgnClip, DeviceContextValues flags);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

            [DllImport("user32.dll")]
            public static extern IntPtr GetDesktopWindow();

            [Flags()]
            public enum RedrawWindowFlags : uint
            {
                /// <summary>
                /// Invalidates the rectangle or region that you specify in lprcUpdate or hrgnUpdate.
                /// You can set only one of these parameters to a non-NULL value. If both are NULL, RDW_INVALIDATE invalidates the entire window.
                /// </summary>
                Invalidate = 0x1,

                /// <summary>Causes the OS to post a WM_PAINT message to the window regardless of whether a portion of the window is invalid.</summary>
                InternalPaint = 0x2,

                /// <summary>
                /// Causes the window to receive a WM_ERASEBKGND message when the window is repainted.
                /// Specify this value in combination with the RDW_INVALIDATE value; otherwise, RDW_ERASE has no effect.
                /// </summary>
                Erase = 0x4,

                /// <summary>
                /// Validates the rectangle or region that you specify in lprcUpdate or hrgnUpdate.
                /// You can set only one of these parameters to a non-NULL value. If both are NULL, RDW_VALIDATE validates the entire window.
                /// This value does not affect internal WM_PAINT messages.
                /// </summary>
                Validate = 0x8,

                NoInternalPaint = 0x10,

                /// <summary>Suppresses any pending WM_ERASEBKGND messages.</summary>
                NoErase = 0x20,

                /// <summary>Excludes child windows, if any, from the repainting operation.</summary>
                NoChildren = 0x40,

                /// <summary>Includes child windows, if any, in the repainting operation.</summary>
                AllChildren = 0x80,

                /// <summary>Causes the affected windows, which you specify by setting the RDW_ALLCHILDREN and RDW_NOCHILDREN values, to receive WM_ERASEBKGND and WM_PAINT messages before the RedrawWindow returns, if necessary.</summary>
                UpdateNow = 0x100,

                /// <summary>
                /// Causes the affected windows, which you specify by setting the RDW_ALLCHILDREN and RDW_NOCHILDREN values, to receive WM_ERASEBKGND messages before RedrawWindow returns, if necessary.
                /// The affected windows receive WM_PAINT messages at the ordinary time.
                /// </summary>
                EraseNow = 0x200,

                Frame = 0x400,

                NoFrame = 0x800
            }

            [DllImport("user32.dll")]
            public static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, RedrawWindowFlags flags);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

            [Flags()]
            public enum SetWindowPosFlags : uint
            {
                /// <summary>If the calling thread and the thread that owns the window are attached to different input queues,
                /// the system posts the request to the thread that owns the window. This prevents the calling thread from
                /// blocking its execution while other threads process the request.</summary>
                /// <remarks>SWP_ASYNCWINDOWPOS</remarks>
                AsynchronousWindowPosition = 0x4000,
                /// <summary>Prevents generation of the WM_SYNCPAINT message.</summary>
                /// <remarks>SWP_DEFERERASE</remarks>
                DeferErase = 0x2000,
                /// <summary>Draws a frame (defined in the window's class description) around the window.</summary>
                /// <remarks>SWP_DRAWFRAME</remarks>
                DrawFrame = 0x0020,
                /// <summary>Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to
                /// the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE
                /// is sent only when the window's size is being changed.</summary>
                /// <remarks>SWP_FRAMECHANGED</remarks>
                FrameChanged = 0x0020,
                /// <summary>Hides the window.</summary>
                /// <remarks>SWP_HIDEWINDOW</remarks>
                HideWindow = 0x0080,
                /// <summary>Does not activate the window. If this flag is not set, the window is activated and moved to the
                /// top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter
                /// parameter).</summary>
                /// <remarks>SWP_NOACTIVATE</remarks>
                DoNotActivate = 0x0010,
                /// <summary>Discards the entire contents of the client area. If this flag is not specified, the valid
                /// contents of the client area are saved and copied back into the client area after the window is sized or
                /// repositioned.</summary>
                /// <remarks>SWP_NOCOPYBITS</remarks>
                DoNotCopyBits = 0x0100,
                /// <summary>Retains the current position (ignores X and Y parameters).</summary>
                /// <remarks>SWP_NOMOVE</remarks>
                IgnoreMove = 0x0002,
                /// <summary>Does not change the owner window's position in the Z order.</summary>
                /// <remarks>SWP_NOOWNERZORDER</remarks>
                DoNotChangeOwnerZOrder = 0x0200,
                /// <summary>Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to
                /// the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent
                /// window uncovered as a result of the window being moved. When this flag is set, the application must
                /// explicitly invalidate or redraw any parts of the window and parent window that need redrawing.</summary>
                /// <remarks>SWP_NOREDRAW</remarks>
                DoNotRedraw = 0x0008,
                /// <summary>Same as the SWP_NOOWNERZORDER flag.</summary>
                /// <remarks>SWP_NOREPOSITION</remarks>
                DoNotReposition = 0x0200,
                /// <summary>Prevents the window from receiving the WM_WINDOWPOSCHANGING message.</summary>
                /// <remarks>SWP_NOSENDCHANGING</remarks>
                DoNotSendChangingEvent = 0x0400,
                /// <summary>Retains the current size (ignores the cx and cy parameters).</summary>
                /// <remarks>SWP_NOSIZE</remarks>
                IgnoreResize = 0x0001,
                /// <summary>Retains the current Z order (ignores the hWndInsertAfter parameter).</summary>
                /// <remarks>SWP_NOZORDER</remarks>
                IgnoreZOrder = 0x0004,
                /// <summary>Displays the window.</summary>
                /// <remarks>SWP_SHOWWINDOW</remarks>
                ShowWindow = 0x0040,
            }

            [DllImport("user32.dll")]
            public static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

            public enum WindowLongFlags : int
            {
                GWL_EXSTYLE = -20,
                GWLP_HINSTANCE = -6,
                GWLP_HWNDPARENT = -8,
                GWL_ID = -12,
                GWL_STYLE = -16,
                GWL_USERDATA = -21,
                GWL_WNDPROC = -4,
                DWLP_USER = 0x8,
                DWLP_MSGRESULT = 0x0,
                DWLP_DLGPROC = 0x4
            }

            [Flags]
            public enum WindowStyles : uint
            {
                WS_OVERLAPPED = 0x00000000,
                WS_POPUP = 0x80000000,
                WS_CHILD = 0x40000000,
                WS_MINIMIZE = 0x20000000,
                WS_VISIBLE = 0x10000000,
                WS_DISABLED = 0x08000000,
                WS_CLIPSIBLINGS = 0x04000000,
                WS_CLIPCHILDREN = 0x02000000,
                WS_MAXIMIZE = 0x01000000,
                WS_BORDER = 0x00800000,
                WS_DLGFRAME = 0x00400000,
                WS_VSCROLL = 0x00200000,
                WS_HSCROLL = 0x00100000,
                WS_SYSMENU = 0x00080000,
                WS_THICKFRAME = 0x00040000,
                WS_GROUP = 0x00020000,
                WS_TABSTOP = 0x00010000,

                WS_MINIMIZEBOX = 0x00020000,
                WS_MAXIMIZEBOX = 0x00010000,

                WS_CAPTION = WS_BORDER | WS_DLGFRAME,
                WS_TILED = WS_OVERLAPPED,
                WS_ICONIC = WS_MINIMIZE,
                WS_SIZEBOX = WS_THICKFRAME,
                WS_TILEDWINDOW = WS_OVERLAPPEDWINDOW,

                WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
                WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,
                WS_CHILDWINDOW = WS_CHILD,

                //Extended Window Styles

                WS_EX_DLGMODALFRAME = 0x00000001,
                WS_EX_NOPARENTNOTIFY = 0x00000004,
                WS_EX_TOPMOST = 0x00000008,
                WS_EX_ACCEPTFILES = 0x00000010,
                WS_EX_TRANSPARENT = 0x00000020,

                //#if(WINVER >= 0x0400)

                WS_EX_MDICHILD = 0x00000040,
                WS_EX_TOOLWINDOW = 0x00000080,
                WS_EX_WINDOWEDGE = 0x00000100,
                WS_EX_CLIENTEDGE = 0x00000200,
                WS_EX_CONTEXTHELP = 0x00000400,

                WS_EX_RIGHT = 0x00001000,
                WS_EX_LEFT = 0x00000000,
                WS_EX_RTLREADING = 0x00002000,
                WS_EX_LTRREADING = 0x00000000,
                WS_EX_LEFTSCROLLBAR = 0x00004000,
                WS_EX_RIGHTSCROLLBAR = 0x00000000,

                WS_EX_CONTROLPARENT = 0x00010000,
                WS_EX_STATICEDGE = 0x00020000,
                WS_EX_APPWINDOW = 0x00040000,

                WS_EX_OVERLAPPEDWINDOW = (WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE),
                WS_EX_PALETTEWINDOW = (WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST),
                //#endif /* WINVER >= 0x0400 */

                //#if(WIN32WINNT >= 0x0500)

                WS_EX_LAYERED = 0x00080000,
                //#endif /* WIN32WINNT >= 0x0500 */

                //#if(WINVER >= 0x0500)

                WS_EX_NOINHERITLAYOUT = 0x00100000, // Disable inheritence of mirroring by children
                WS_EX_LAYOUTRTL = 0x00400000, // Right to left mirroring
                                              //#endif /* WINVER >= 0x0500 */

                //#if(WIN32WINNT >= 0x0500)

                WS_EX_COMPOSITED = 0x02000000,
                WS_EX_NOACTIVATE = 0x08000000
                //#endif /* WIN32WINNT >= 0x0500 */

            }


            [DllImport("user32.dll")]
            public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

            [DllImport("user32.dll")]
            public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

            public enum ShowWindowCommands : int
            {
                /// <summary>
                /// Hides the window and activates another window.
                /// </summary>
                Hide = 0,
                /// <summary>
                /// Activates and displays a window. If the window is minimized or
                /// maximized, the system restores it to its original size and position.
                /// An application should specify this flag when displaying the window
                /// for the first time.
                /// </summary>
                Normal = 1,
                /// <summary>
                /// Activates the window and displays it as a minimized window.
                /// </summary>
                ShowMinimized = 2,
                /// <summary>
                /// Maximizes the specified window.
                /// </summary>
                Maximize = 3, // is this the right value?
                /// <summary>
                /// Activates the window and displays it as a maximized window.
                /// </summary>      
                ShowMaximized = 3,
                /// <summary>
                /// Displays a window in its most recent size and position. This value
                /// is similar to <see cref="Win32.ShowWindowCommand.Normal"/>, except
                /// the window is not activated.
                /// </summary>
                ShowNoActivate = 4,
                /// <summary>
                /// Activates the window and displays it in its current size and position.
                /// </summary>
                Show = 5,
                /// <summary>
                /// Minimizes the specified window and activates the next top-level
                /// window in the Z order.
                /// </summary>
                Minimize = 6,
                /// <summary>
                /// Displays the window as a minimized window. This value is similar to
                /// <see cref="Win32.ShowWindowCommand.ShowMinimized"/>, except the
                /// window is not activated.
                /// </summary>
                ShowMinNoActive = 7,
                /// <summary>
                /// Displays the window in its current size and position. This value is
                /// similar to <see cref="Win32.ShowWindowCommand.Show"/>, except the
                /// window is not activated.
                /// </summary>
                ShowNA = 8,
                /// <summary>
                /// Activates and displays the window. If the window is minimized or
                /// maximized, the system restores it to its original size and position.
                /// An application should specify this flag when restoring a minimized window.
                /// </summary>
                Restore = 9,
                /// <summary>
                /// Sets the show state based on the SW_* value specified in the
                /// STARTUPINFO structure passed to the CreateProcess function by the
                /// program that started the application.
                /// </summary>
                ShowDefault = 10,
                /// <summary>
                ///  <b>Windows 2000/XP:</b> Minimizes a window, even if the thread
                /// that owns the window is not responding. This flag should only be
                /// used when minimizing windows from a different thread.
                /// </summary>
                ForceMinimize = 11
            }
        }

    }
}
