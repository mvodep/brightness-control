namespace DisplayBrightness.Views
{
    public sealed partial class MainPage : Page
    {
        public System.Collections.ObjectModel.ObservableCollection<DisplayInfo> Displays { get; } = new();

        public MainPage()
        {
            this.InitializeComponent();

            RefreshDisplays();
        }

        public void RefreshDisplays()
        {
            Displays.Clear();
            
            var wmiService = new DisplayWmmiService();
            var wmiDisplays = wmiService.GetDisplays();
            
            foreach (var d in wmiDisplays)
            {
                Displays.Add(d);
            }

            var win32Displays = DisplayService.GetDisplays();    
            
            foreach (var d in win32Displays)
            {
                bool exists = ProcessWin32Display(d, wmiDisplays);
                
                if (!exists)
                {
                    Displays.Add(d);
                }
            }
        }

        private bool ProcessWin32Display(DisplayInfo d, System.Collections.Generic.List<DisplayInfo> wmiDisplays)
        {
            foreach (var w in wmiDisplays)
            {
                if (IsSameMonitor(w.MonitorId, d.MonitorId))
                {
                    w.DeviceName = d.DeviceName;
                    
                    w.SetNightLightCallback = (val) => DisplayService.SetNightLight(w.DeviceName, val);
                    
                    return true;
                }
            }

            return false;
        }
        
        private bool IsSameMonitor(string? wmiId, string? win32Id)
        {
            if (string.IsNullOrEmpty(wmiId) || string.IsNullOrEmpty(win32Id))
            {
                return false;
            }
            
            try
            {
                var partsW = wmiId.Split('\\');
                var parts32 = win32Id.Split('\\');
                
                if (partsW.Length > 1 && parts32.Length > 1)
                {
                    return string.Equals(partsW[1], parts32[1], StringComparison.OrdinalIgnoreCase);
                }
            }
            catch 
            {
            }
            
            return false;
        }

        private async void Slider_PointerCaptureLost(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Slider slider && slider.Tag is DisplayInfo info)
            {
                int newValue = (int)slider.Value;
                
                await System.Threading.Tasks.Task.Run(() =>
                {
                   info.SetBrightnessCallback?.Invoke(newValue);
                });
            }
        }

        private async void GlobalSlider_PointerCaptureLost(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is Slider slider)
            {
                double percent = slider.Value / 100.0;
                
                var displaysToUpdate = new System.Collections.Generic.List<(DisplayInfo Info, int NewValue)>();

                foreach (var d in Displays)
                {
                    if (d.IsBrightnessSupported)
                    {
                        int range = d.MaxBrightness - d.MinBrightness;
                        int newVal = d.MinBrightness + (int)(range * percent);
                        
                        if (newVal < d.MinBrightness)
                        {
                            newVal = d.MinBrightness;
                        }

                        if (newVal > d.MaxBrightness)
                        {
                            newVal = d.MaxBrightness;
                        }

                        d.Brightness = newVal;
                        displaysToUpdate.Add((d, newVal));
                    }
                }

                await System.Threading.Tasks.Task.Run(() =>
                {
                    foreach (var item in displaysToUpdate)
                    {
                        item.Info.SetBrightnessCallback?.Invoke(item.NewValue);
                    }
                });
            }
        }

        private void GlobalNightLight_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is Microsoft.UI.Xaml.Controls.Primitives.ToggleButton toggle)
            {
                bool isEnabled = toggle.IsChecked ?? false;
                
                foreach (var d in Displays)
                {
                    d.IsNightLightEnabled = isEnabled;
                }
            }
        }
    }
}
