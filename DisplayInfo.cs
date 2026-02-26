using System;

namespace DisplayBrightness
{
    public class DisplayInfo : System.ComponentModel.INotifyPropertyChanged
    {
        public string? FriendlyName { get; set; }
        public string? DeviceName { get; set; }
        public string? MonitorId { get; set; }
        
        public bool IsBrightnessSupported { get; set; }
        
        public Microsoft.UI.Xaml.Visibility DdcCiErrorVisibility => IsBrightnessSupported ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;

        public int MinBrightness { get; set; } = 0;

        public int MaxBrightness { get; set; } = 100;
        
        public Action<int>? SetBrightnessCallback { get; set; }
        public Action<bool>? SetNightLightCallback { get; set; }

        private int _brightness;
        public int Brightness
        {
            get => _brightness;
            set
            {
                if (_brightness != value)
                {
                    _brightness = value;

                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Brightness)));
                }
            }
        }

        private bool _isNightLightEnabled;
        public bool IsNightLightEnabled
        {
            get => _isNightLightEnabled;
            set
            {
                if (_isNightLightEnabled != value)
                {
                    _isNightLightEnabled = value;

                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsNightLightEnabled)));

                    SetNightLightCallback?.Invoke(value);
                }
            }
        }

        public IntPtr PhysicalMonitorHandle;
        public uint PhysicalMonitorCount;
        public object? PhysicalMonitors;

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }
}
