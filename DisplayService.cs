using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DisplayBrightness
{
    public static class DisplayService
    {
        /// <summary>
        /// Retrieves a list of all detected displays and their capabilities.
        /// </summary>
        public static List<DisplayInfo> GetDisplays()
        {
            var displays = new List<DisplayInfo>();

            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.Rect lprcMonitor, IntPtr dwData)
                {
                    var info = GetDisplayInfo(hMonitor);

                    if (info != null)
                    {
                        displays.Add(info);
                    }

                    return true;
                }, IntPtr.Zero);

            return displays;
        }

        /// <summary>
        /// Retrieves detailed information for a specific monitor handle.
        /// </summary>
        private static DisplayInfo? GetDisplayInfo(IntPtr hMonitor)
        {
            if (!TryGetMonitorInfoEx(hMonitor, out var mi))
            {
                return null;
            }

            string? monitorId = GetMonitorDeviceId(mi.DeviceName);

            var info = new DisplayInfo
            {
                DeviceName = mi.DeviceName,
                FriendlyName = GetFriendlyName(mi.DeviceName),
                MonitorId = monitorId
            };

            TryPopulateBrightnessFeatures(hMonitor, info);
            AssignNightLightCallback(info);

            return info;
        }

        /// <summary>
        /// Attempts to get extended monitor information.
        /// </summary>
        private static bool TryGetMonitorInfoEx(IntPtr hMonitor, out NativeMethods.MONITORINFOEX mi)
        {
            mi = new NativeMethods.MONITORINFOEX();
            mi.Size = Marshal.SizeOf(mi);

            return NativeMethods.GetMonitorInfo(hMonitor, ref mi);
        }

        /// <summary>
        /// Retrieves the device ID for a given monitor device name.
        /// </summary>
        private static string? GetMonitorDeviceId(string deviceName)
        {
            NativeMethods.DISPLAY_DEVICE monitorDd = new NativeMethods.DISPLAY_DEVICE();
            monitorDd.cb = Marshal.SizeOf(monitorDd);

            if (NativeMethods.EnumDisplayDevices(deviceName, 0, ref monitorDd, 0))
            {
                return monitorDd.DeviceID;
            }

            return null;
        }

        /// <summary>
        /// Assigns the night light callback to the display info if a device name is present.
        /// </summary>
        private static void AssignNightLightCallback(DisplayInfo info)
        {
            if (string.IsNullOrEmpty(info.DeviceName))
            {
                return;
            }

            string devName = info.DeviceName;
            
            info.SetNightLightCallback = (val) =>
            {
                SetNightLight(devName, val);
            };
        }

        /// <summary>
        /// Attempts to populate hardware brightness control features for the specified monitor.
        /// </summary>
        private static void TryPopulateBrightnessFeatures(IntPtr hMonitor, DisplayInfo info)
        {
            if (!TryGetPhysicalMonitors(hMonitor, out uint count, out var physicalMonitors))
            {
                return;
            }

            TryReadAndAssignBrightness(info, physicalMonitors, count);
        }

        /// <summary>
        /// Attempts to allocate and retrieve physical monitors for a given logical monitor handle.
        /// </summary>
        private static bool TryGetPhysicalMonitors(IntPtr hMonitor, out uint count, out NativeMethods.PHYSICAL_MONITOR[] physicalMonitors)
        {
            count = 0;
            physicalMonitors = Array.Empty<NativeMethods.PHYSICAL_MONITOR>();

            try
            {
                if (!NativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out count) || count == 0)
                {
                    return false;
                }

                physicalMonitors = new NativeMethods.PHYSICAL_MONITOR[count];

                if (!NativeMethods.GetPhysicalMonitorsFromHMONITOR(hMonitor, count, physicalMonitors))
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to read the monitor brightness values and assigns them to the display info object.
        /// </summary>
        private static void TryReadAndAssignBrightness(DisplayInfo info, NativeMethods.PHYSICAL_MONITOR[] physicalMonitors, uint count)
        {
            try
            {
                info.PhysicalMonitorHandle = physicalMonitors[0].hPhysicalMonitor;
                info.PhysicalMonitorCount = count;
                info.PhysicalMonitors = physicalMonitors;

                if (!TryGetBrightnessBounds(info.PhysicalMonitorHandle, out uint minB, out uint curB, out uint maxB))
                {
                    info.IsBrightnessSupported = false;

                    return;
                }

                AssignBrightnessProperties(info, minB, curB, maxB);
            }
            finally
            {
                NativeMethods.DestroyPhysicalMonitors(count, physicalMonitors);
                info.PhysicalMonitorHandle = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Safely attempts to extract the minimum, current, and maximum brightness boundaries.
        /// </summary>
        private static bool TryGetBrightnessBounds(IntPtr handle, out uint minB, out uint curB, out uint maxB)
        {
            minB = 0;
            curB = 0;
            maxB = 0;

            try
            {
                if (!NativeMethods.GetMonitorBrightness(handle, out minB, out curB, out maxB))
                {
                    return false;
                }

                if (minB == maxB || maxB == 0)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Assigns the retrieved brightness boundaries and initializes the brightness callback.
        /// </summary>
        private static void AssignBrightnessProperties(DisplayInfo info, uint minB, uint curB, uint maxB)
        {
            info.MinBrightness = (int)minB;
            info.MaxBrightness = (int)maxB;
            info.Brightness = (int)curB;
            info.IsBrightnessSupported = true;
            
            string devName = info.DeviceName ?? "";
            
            info.SetBrightnessCallback = (val) => 
            { 
                SetBrightnessAndContrast(devName, val);
            };
        }

        /// <summary>
        /// Sets both brightness and contrast levels for a specific display device.
        /// </summary>
        public static void SetBrightnessAndContrast(string deviceName, int value)
        {
            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.Rect lprcMonitor, IntPtr dwData)
                {
                    return ProcessMonitorForBrightnessChange(hMonitor, deviceName, value);
                }, IntPtr.Zero);
        }

        /// <summary>
        /// Processes a single monitor handle during enumeration to apply a brightness change if it matches the target device name.
        /// </summary>
        private static bool ProcessMonitorForBrightnessChange(IntPtr hMonitor, string targetDeviceName, int value)
        {
            if (!IsTargetMonitor(hMonitor, targetDeviceName))
            {
                return true;
            }

            ApplyBrightnessToPhysicalMonitors(hMonitor, value);

            return false;
        }

        /// <summary>
        /// Checks if the provided monitor handle matches the target device name.
        /// </summary>
        private static bool IsTargetMonitor(IntPtr hMonitor, string targetDeviceName)
        {
            if (!TryGetMonitorInfoEx(hMonitor, out var mi))
            {
                return false;
            }

            return mi.DeviceName == targetDeviceName;
        }

        /// <summary>
        /// Retrieves the physical monitors for a handle and applies the brightness and contrast values.
        /// </summary>
        private static void ApplyBrightnessToPhysicalMonitors(IntPtr hMonitor, int value)
        {
            if (!TryGetPhysicalMonitors(hMonitor, out uint count, out var physicalMonitors))
            {
                return;
            }

            try
            {
                NativeMethods.SetMonitorBrightness(physicalMonitors[0].hPhysicalMonitor, (uint)value);
                NativeMethods.SetMonitorContrast(physicalMonitors[0].hPhysicalMonitor, (uint)value);
            }
            finally
            {
                NativeMethods.DestroyPhysicalMonitors(count, physicalMonitors);
            }
        }

        /// <summary>
        /// Retrieves the friendly name for a display device.
        /// </summary>
        private static string GetFriendlyName(string deviceName)
        {
            NativeMethods.DISPLAY_DEVICE dd = new NativeMethods.DISPLAY_DEVICE();
            dd.cb = Marshal.SizeOf(dd);
            
            if (!NativeMethods.EnumDisplayDevices(deviceName, 0, ref dd, 0))
            {
                return "Unknown Display";
            }

            if ((dd.StateFlags & 1) == 0) 
            {
                return "Unknown Display";
            }

            NativeMethods.DISPLAY_DEVICE monitorDd = new NativeMethods.DISPLAY_DEVICE();
            monitorDd.cb = Marshal.SizeOf(monitorDd);

            if (!NativeMethods.EnumDisplayDevices(deviceName, 0, ref monitorDd, 0))
            {
                return "Unknown Display";
            }

            return monitorDd.DeviceString;
        }

        /// <summary>
        /// Applies the night light gamma ramp to the specified display device.
        /// </summary>
        public static void SetNightLight(string? deviceName, bool enable)
        {
            if (string.IsNullOrEmpty(deviceName))
            {
                return;
            }
            
            IntPtr hdc = NativeMethods.CreateDC(null, deviceName, null, IntPtr.Zero);

            if (hdc == IntPtr.Zero)
            {
                return;
            }

            try
            {
                NativeMethods.RAMP ramp = CreateGammaRamp(enable);
                NativeMethods.SetDeviceGammaRamp(hdc, ref ramp);
            }
            finally
            {
                NativeMethods.DeleteDC(hdc);
            }
        }

        /// <summary>
        /// Generates the appropriate gamma ramp values depending on whether night light is enabled.
        /// </summary>
        private static NativeMethods.RAMP CreateGammaRamp(bool enable)
        {
            NativeMethods.RAMP ramp = new NativeMethods.RAMP();
            ramp.Red = new ushort[256];
            ramp.Green = new ushort[256];
            ramp.Blue = new ushort[256];

            for (int i = 0; i < 256; i++)
            {
                double val = (i / 255.0) * 65535;

                double redMult = enable ? SettingsService.NightLightRed : 1.0;
                double greenMult = enable ? SettingsService.NightLightGreen : 1.0;
                double blueMult = enable ? SettingsService.NightLightBlue : 1.0;

                ramp.Red[i] = (ushort)Math.Min(65535, val * redMult);
                ramp.Green[i] = (ushort)Math.Min(65535, val * greenMult);
                ramp.Blue[i] = (ushort)Math.Min(65535, val * blueMult);
            }

            return ramp;
        }
    }
}
