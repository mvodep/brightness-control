using System;
using System.Runtime.InteropServices;

namespace DisplayBrightness
{
    public class TrayIcon : IDisposable
    {
        private const uint NIM_ADD = 0x00000000;
        private const uint NIM_DELETE = 0x00000002;
        private const uint NIF_MESSAGE = 0x00000001;
        private const uint NIF_ICON = 0x00000002;
        private const uint NIF_TIP = 0x00000004;
        private const uint NIF_GUID = 0x00000020;
        private const uint WM_LBUTTONUP = 0x0202;
        private const int WM_USER = 0x0400;
        private const int TRAY_Callback = WM_USER + 1001;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uID;
            public int uFlags;
            public int uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public int dwState;
            public int dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public int uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public int dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        private const uint MF_STRING = 0x00000000;
        private const uint TPM_RETURNCMD = 0x0100;
        private const uint TPM_NONOTIFY = 0x0080;
        private const int WM_RBUTTONUP = 0x0205;

        private Action _onExit;

        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const int GWLP_WNDPROC = -4;
        private IntPtr _hWnd;
        private IntPtr _oldWndProc;
        private WndProcDelegate _newWndProcDelegate;
        private NOTIFYICONDATA _nid;
        private Action _onClick;
        private Action _onAdjustNightMode;

        public delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private static readonly Guid TrayIconGuid = new Guid("6D50607A-2616-4328-8C37-ED2E07361734");

        public TrayIcon(IntPtr hWnd, Action onClick, Action onAdjustNightMode, Action onExit)
        {
            _hWnd = hWnd;
            _onClick = onClick;
            _onAdjustNightMode = onAdjustNightMode;
            _onExit = onExit;
            _newWndProcDelegate = new WndProcDelegate(WndProc);
            _oldWndProc = SetWindowLong(_hWnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProcDelegate));

            _nid = new NOTIFYICONDATA();
            _nid.cbSize = Marshal.SizeOf(_nid);
            _nid.hWnd = _hWnd;
            _nid.uID = 100;
            _nid.uFlags = (int)(NIF_ICON | NIF_TIP | NIF_MESSAGE | NIF_GUID);
            _nid.uCallbackMessage = TRAY_Callback;
            
            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
            
            if (System.IO.File.Exists(iconPath))
            {
                _nid.hIcon = LoadImage(IntPtr.Zero, iconPath, 1, 0, 0, 0x00000010);
            }
            else
            {
                _nid.hIcon = LoadIcon(IntPtr.Zero, (IntPtr)32512);
            }

            _nid.szTip = "DisplayBrightness";
            _nid.guidItem = TrayIconGuid;

            Shell_NotifyIcon(NIM_ADD, ref _nid);
        }

        private const int WM_DISPLAYCHANGE = 0x007E;
        private const int WM_DEVICECHANGE = 0x0219;

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == TRAY_Callback)
            {
                if ((long)lParam == WM_LBUTTONUP)
                {
                    _onClick?.Invoke();
                }
                else if ((long)lParam == WM_RBUTTONUP)
                {
                    ShowContextMenu();
                }
            }
            else if (msg == WM_DISPLAYCHANGE || msg == WM_DEVICECHANGE)
            {
                OnDisplayChange?.Invoke();
            }
            
            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        private const uint MF_CHECKED = 0x00000008;
        private const uint MF_UNCHECKED = 0x00000000;
        private const uint MF_SEPARATOR = 0x00000800;

        private void ShowContextMenu()
        {
            var hMenu = CreatePopupMenu();
            
            if (hMenu == IntPtr.Zero)
            {
                return;
            }

            bool isStartup = CheckStartupRegistry();
            uint flags = MF_STRING | (isStartup ? MF_CHECKED : MF_UNCHECKED);
            
            AppendMenu(hMenu, flags, 2, "Run at Startup");
            AppendMenu(hMenu, MF_STRING, 3, "Adjust Night Mode");
            AppendMenu(hMenu, MF_SEPARATOR, 0, "");
            AppendMenu(hMenu, MF_STRING, 1, "Exit");

            GetCursorPos(out POINT pt);
            SetForegroundWindow(_hWnd);

            int cmd = TrackPopupMenu(hMenu, TPM_RETURNCMD | TPM_NONOTIFY, pt.X, pt.Y, 0, _hWnd, IntPtr.Zero);

            if (cmd == 1)
            {
                _onExit?.Invoke();
            }
            else if (cmd == 2)
            {
                ToggleStartupRegistry(!isStartup);
            }
            else if (cmd == 3)
            {
                _onAdjustNightMode?.Invoke();
            }

            DestroyMenu(hMenu);
        }

        private const string StartupKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string AppName = "DisplayBrightness";

        private bool CheckStartupRegistry()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(StartupKey, false);
                
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }

        private void ToggleStartupRegistry(bool enable)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(StartupKey, true);

                if (key == null)
                {
                    return;
                }

                if (enable)
                {
                    string path = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    
                    if (!string.IsNullOrEmpty(path))
                    {
                        key.SetValue(AppName, $"\"{path}\"");
                    }
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
            catch
            {
            }
        }

        public event Action? OnDisplayChange;

        private IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr newProc)
        {
            if (IntPtr.Size == 8)
            {
                return SetWindowLongPtr64(hWnd, nIndex, newProc);
            }
            else
            {
                return (IntPtr)SetWindowLong32(hWnd, nIndex, (int)newProc);
            }
        }

        public void Dispose()
        {
            Shell_NotifyIcon(NIM_DELETE, ref _nid);
            
            if (_oldWndProc != IntPtr.Zero)
            {
                SetWindowLong(_hWnd, GWLP_WNDPROC, _oldWndProc);
                _oldWndProc = IntPtr.Zero;
            }
        }
    }
}
