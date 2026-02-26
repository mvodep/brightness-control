using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DisplayBrightness.Views
{
    public sealed partial class NightModeWindow : Window
    {
        private bool _isInitializing = true;

        public NightModeWindow()
        {
            this.InitializeComponent();

            WindowHelper.ConfigureStyle(this);

            this.Activated += (s, args) =>
            {
                if (args.WindowActivationState == WindowActivationState.Deactivated)
                {
                    this.Close();
                }
            };

            if (SettingsService.NightLightRed < 0.5) SettingsService.NightLightRed = 0.5;
            if (SettingsService.NightLightGreen < 0.5) SettingsService.NightLightGreen = 0.5;
            if (SettingsService.NightLightBlue < 0.5) SettingsService.NightLightBlue = 0.5;

            RedSlider.Value = SettingsService.NightLightRed;
            GreenSlider.Value = SettingsService.NightLightGreen;
            BlueSlider.Value = SettingsService.NightLightBlue;

            _isInitializing = false;

        }

        private async void Slider_PointerCaptureLost(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (_isInitializing)
            {
                return;
            }

            SettingsService.NightLightRed = RedSlider.Value;
            SettingsService.NightLightGreen = GreenSlider.Value;
            SettingsService.NightLightBlue = BlueSlider.Value;

            SettingsService.SaveNightLightSettings();

            if (NightModeToggle.IsOn)
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    ApplyToAll(true);
                });
            }
        }

        private async void NightModeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            bool isOn = NightModeToggle.IsOn;
            
            await System.Threading.Tasks.Task.Run(() =>
            {
                ApplyToAll(isOn);
            });
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _isInitializing = true;

            RedSlider.Value = 1.0;
            GreenSlider.Value = 0.9;
            BlueSlider.Value = 0.5;

            SettingsService.NightLightRed = 1.0;
            SettingsService.NightLightGreen = 0.9;
            SettingsService.NightLightBlue = 0.5;

            SettingsService.SaveNightLightSettings();

            _isInitializing = false;

            if (NightModeToggle.IsOn)
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    ApplyToAll(true);
                });
            }
        }

        private void ApplyToAll(bool state)
        {
            var displays = DisplayService.GetDisplays();

            foreach (var d in displays)
            {
                if (!string.IsNullOrEmpty(d.DeviceName))
                {
                    DisplayService.SetNightLight(d.DeviceName, state);
                }
            }
        }
    }
}
