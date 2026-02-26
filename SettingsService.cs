using System;

namespace DisplayBrightness
{
    public static class SettingsService
    {
        public static double NightLightRed { get; set; } = 1.0;
        public static double NightLightGreen { get; set; } = 0.9;
        public static double NightLightBlue { get; set; } = 0.5;

        private static string GetSettingsFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = System.IO.Path.Combine(appData, "DisplayBrightness");
            
            if (!System.IO.Directory.Exists(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
            }
            
            return System.IO.Path.Combine(folder, "settings.json");
        }

        public class SettingsData
        {
            public double NightLightRed { get; set; } = 1.0;
            public double NightLightGreen { get; set; } = 0.9;
            public double NightLightBlue { get; set; } = 0.5;
        }

        static SettingsService()
        {
            LoadNightLightSettings();
        }

        public static void LoadNightLightSettings()
        {
            try
            {
                string path = GetSettingsFilePath();
                
                if (System.IO.File.Exists(path))
                {
                    string json = System.IO.File.ReadAllText(path);
                    var data = System.Text.Json.JsonSerializer.Deserialize<SettingsData>(json);
                    
                    if (data != null)
                    {
                        NightLightRed = data.NightLightRed;
                        NightLightGreen = data.NightLightGreen;
                        NightLightBlue = data.NightLightBlue;
                    }
                }
            }
            catch
            {
            }
        }

        public static void SaveNightLightSettings()
        {
            try
            {
                string path = GetSettingsFilePath();
                var data = new SettingsData
                {
                    NightLightRed = NightLightRed,
                    NightLightGreen = NightLightGreen,
                    NightLightBlue = NightLightBlue
                };

                var options = new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                };
                
                string json = System.Text.Json.JsonSerializer.Serialize(data, options);
                System.IO.File.WriteAllText(path, json);
            }
            catch
            {
            }
        }
    }
}
