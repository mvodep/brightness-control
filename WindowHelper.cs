using System;
using Microsoft.UI.Xaml;

namespace DisplayBrightness
{
    public static class WindowHelper
    {
        [System.Runtime.InteropServices.DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        /// <summary>
        /// Configures the window with acrylic backdrop, rounded corners and fixed size.
        /// </summary>
        public static void ConfigureStyle(Window window)
        {
            if (Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
            {
                var backdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
                window.SystemBackdrop = backdrop;
            }

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            int cornerPreference = 2;
            
            DwmSetWindowAttribute(hWnd, 33, ref cornerPreference, sizeof(int));

            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                var presenter = Microsoft.UI.Windowing.OverlappedPresenter.Create();
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
                presenter.IsResizable = false;
                presenter.SetBorderAndTitleBar(true, false);
                appWindow.SetPresenter(presenter);
                appWindow.IsShownInSwitchers = false;
                appWindow.Resize(new Windows.Graphics.SizeInt32(350, 500));
            }
        }

        /// <summary>
        /// Positions the AppWindow near the current cursor position.
        /// </summary>
        public static bool PositionNearCursor(Microsoft.UI.Windowing.AppWindow appWindow)
        {
            if (!GetCursorPos(out POINT point))
            {
                return false;
            }

            int w = 350;
            int h = 500;
             
            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromPoint(
                new Windows.Graphics.PointInt32(point.X, point.Y), 
                Microsoft.UI.Windowing.DisplayAreaFallback.Primary);

            int x = point.X - (w / 2);
            int y = point.Y - h - 10;
             
            if (displayArea != null)
            {
                if (x + w > displayArea.WorkArea.X + displayArea.WorkArea.Width)
                {
                    x = (displayArea.WorkArea.X + displayArea.WorkArea.Width) - w;
                }

                if (x < displayArea.WorkArea.X)
                {
                    x = displayArea.WorkArea.X;
                }

                if (y < displayArea.WorkArea.Y)
                {
                    y = point.Y + 10;
                }

                if (y + h > displayArea.WorkArea.Y + displayArea.WorkArea.Height)
                {
                    y = (displayArea.WorkArea.Y + displayArea.WorkArea.Height) - h;
                }
            }
            
            appWindow?.MoveAndResize(new Windows.Graphics.RectInt32(x, y, w, h));

            return true;
        }
    }
}
