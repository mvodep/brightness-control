using Microsoft.UI.Xaml.Navigation;

namespace DisplayBrightness
{
    public partial class App : Application
    {
        private Window window = Window.Current;
        private TrayIcon? _trayIcon;
        private Views.NightModeWindow? _nightModeWindow;

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private System.Threading.Mutex? _mutex;
        
        public App()
        {
            System.Environment.SetEnvironmentVariable("MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY", System.AppContext.BaseDirectory);

            bool createdNew;
            _mutex = new System.Threading.Mutex(true, "DisplayBrightness_SingleInstance_Mutex", out createdNew);
            
            if (!createdNew)
            {
                InstanceCommunicator.SignalExistingInstance();
                System.Diagnostics.Process.GetCurrentProcess().Kill();

                return;
            }

            this.InitializeComponent();
            
            InstanceCommunicator.StartNamedPipeServer(() => 
            {
                window?.DispatcherQueue.TryEnqueue(() => 
                {
                    ShowAppWindow();
                });
            });
        }



        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        private void ShowAppWindow()
        {
            if (window == null)
            {
                return;
            }

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            ShowWindow(hWnd, SW_RESTORE);

            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
            
            if (displayArea != null)
            {
                int w = 350;
                int h = 500;

                int x = (displayArea.WorkArea.X + displayArea.WorkArea.Width) - w - 10;
                int y = (displayArea.WorkArea.Y + displayArea.WorkArea.Height) - h - 10;
                
                appWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, w, h));
            }

            appWindow?.Show();
            SetForegroundWindow(hWnd);
            window.Activate();
        }

        private void ShowNightModeWindow()
        {
            if (_nightModeWindow == null)
            {
                _nightModeWindow = new Views.NightModeWindow();
                _nightModeWindow.Closed += (s, e) => _nightModeWindow = null;
            }

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(_nightModeWindow);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                WindowHelper.PositionNearCursor(appWindow);
            }

            ShowWindow(hWnd, SW_RESTORE);
            SetForegroundWindow(hWnd);
            _nightModeWindow.Activate();
        }

        private void ToggleAppWindow()
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (window.Visible)
            {
                appWindow?.Hide();

                return;
            }

            if (appWindow != null)
            {
                WindowHelper.PositionNearCursor(appWindow);
            }

            appWindow?.Show();
            SetForegroundWindow(hWnd);
            window.Activate();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            window ??= new Window();
            window.Title = "DisplayBrightness";
            
            WindowHelper.ConfigureStyle(window);

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            string iconPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
            
            if (System.IO.File.Exists(iconPath))
            {
                appWindow.SetIcon(iconPath);
            }

            _trayIcon = new TrayIcon(hWnd, () =>
            {
                ToggleAppWindow();
            },
            () =>
            {
                window.DispatcherQueue.TryEnqueue(() => 
                {
                    ShowNightModeWindow();
                });
            },
            () => 
            {
                 _trayIcon?.Dispose();
                 System.Environment.Exit(0);
            });
            
             _trayIcon.OnDisplayChange += () =>
            {
                window.DispatcherQueue.TryEnqueue(() =>
                {
                    if (window.Content is Frame f && f.Content is MainPage mp)
                    {
                        mp.RefreshDisplays();
                    }
                });
            };
            
            if (appWindow != null)
            {
                appWindow.Closing += (s, args) =>
                {
                    args.Cancel = true;
                    appWindow.Hide();
                };
            }
            
            window.Activated += (s, args) =>
            {
                if (args.WindowActivationState == WindowActivationState.Deactivated)
                {
                    appWindow?.Hide();
                }
            };

            if (window.Content is not Frame rootFrame)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                window.Content = rootFrame;
            }

            _ = rootFrame.Navigate(typeof(MainPage), e.Arguments);
            
            appWindow?.Hide(); 
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
    }
}
