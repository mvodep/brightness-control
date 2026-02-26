using System;
using System.Collections.Generic;
using System.Management;

namespace DisplayBrightness
{
    public class DisplayWmmiService
    {
        /// <summary>
        /// Retrieves a list of all displays supporting brightness control via Windows Management Instrumentation (WMI).
        /// </summary>
        public List<DisplayInfo> GetDisplays()
        {
            var displays = new List<DisplayInfo>();

            try
            {
                using var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM WmiMonitorBrightness");
                using var results = searcher.Get();

                foreach (ManagementObject queryObj in results)
                {
                    using (queryObj)
                    {
                        var info = new DisplayInfo
                        {
                            DeviceName = queryObj["InstanceName"]?.ToString() ?? "Unknown WMI",
                            FriendlyName = "Internal Display",
                            MonitorId = queryObj["InstanceName"]?.ToString(),
                            IsBrightnessSupported = true,
                            Brightness = Convert.ToInt32(queryObj["CurrentBrightness"]),
                            MinBrightness = 0,
                            MaxBrightness = 100
                        };
                        
                        string instanceName = info.MonitorId ?? "";
                        info.SetBrightnessCallback = (val) => new DisplayWmmiService().SetBrightness(instanceName, val);

                        displays.Add(info);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WMI Error: {ex.Message}");
            }

            return displays;
        }

        /// <summary>
        /// Sets the brightness level for a specific WMI monitor instance.
        /// </summary>
        /// <param name="instanceName">The WMI instance name of the monitor.</param>
        /// <param name="brightness">The target brightness level (0-100).</param>
        public void SetBrightness(string instanceName, int brightness)
        {
            try
            {
                string query = $"SELECT * FROM WmiMonitorBrightnessMethods WHERE Active=True AND InstanceName='{instanceName.Replace("\\", "\\\\")}'";
                using var searcher = new ManagementObjectSearcher("root\\WMI", query);
                using var results = searcher.Get();

                foreach (ManagementObject queryObj in results)
                {
                    using (queryObj)
                    {
                        queryObj.InvokeMethod("WmiSetBrightness", new object[] { 1, brightness });
                    }

                    break;
                }
            }
            catch (Exception ex)
            {
                 System.Diagnostics.Debug.WriteLine($"WMI Set Error: {ex.Message}");
            }
        }
    }
}
