using System.Text.Json;
using System.Xml.Linq;

namespace NugetCleaner
{
    internal static class Config
    {
        private const string ConfigFilePath = "NugetCleaner.cfg";

        static Config()
        {
            if (File.Exists(ConfigFilePath))
            {
                LoadConfig();
            }
            else
            {
                WriteDefaultConfig();
            }
        }
        public static Settings Settings { get; set; }

        private static void LoadConfig()
        {
            var jsonData = File.ReadAllText(ConfigFilePath);
            var configObj = JsonSerializer.Deserialize<Settings>(jsonData);

            if (configObj != null)
            {
                Settings = configObj;
            }
        }

        private static void WriteDefaultConfig()
        {
            Settings = new Settings();
            JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // For better readability
            };

            var defaultConfigContent = JsonSerializer.Serialize(Settings, jsonSerializerOptions);
            File.WriteAllText(ConfigFilePath, defaultConfigContent);
        }
    }
    public class Settings
    {
        public string MoveToSubdirs_Doc { get; set; } = "If set to true, organizes packages in folders";
        /// <summary>
        /// If set to true, organizes packages in folders
        /// </summary>
        public bool MoveToSubdirs { get; set; } = true;

        public string VersionRevision_Doc { get; set; } =
            "Defines the extent to which the software should clean" + Environment.NewLine +
            "Possible values:" + Environment.NewLine +
            "- major: only keep the highest full versions, eg 1.3.3; 2.0.1; 3.0.0" + Environment.NewLine +
            "- minor: keep each highest minor, eg 0.0.0; 0.1.3; 0.2.2" + Environment.NewLine +
            "- build: keep each revision, eg 0.0.0.0; 0.0.1.23; 0.0.2.33";
        /// <summary>
        /// Defines the extent to which the software should clean
        /// </summary>
        /// <remarks>
        /// Possible values:<br/>
        /// - major: only keep the highest full versions, eg 1.3.3; 2.0.1; 3.0.0
        /// - minor: keep each highest minor, eg 0.0.0; 0.1.3; 0.2.2
        /// - build: keep each revision, eg 0.0.0.0; 0.0.1.23; 0.0.2.33
        /// </remarks>
        public string VersionRevision
        {
            get
            {
                return _VersionRevision.ToUpperInvariant().Trim();
            }
            set
            {
                _VersionRevision = value;
            }
        } 
        private string _VersionRevision = "build";
    }
}
