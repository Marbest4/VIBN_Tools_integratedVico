using System.IO;
using System.Text.Json;

namespace VIBN_Tools.ContainerGeneration.AI
{
    /// <summary>
    /// Globale Konfiguration für den Noise-Filter.
    /// Wird vom NoiseFilterEditor geladen/gespeichert.
    /// </summary>
    public sealed class NoiseFilterConfig
    {
        public List<string> BadWords { get; set; } = new();

        public static string ConfigFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "VIBN_Tools", "NoiseFilter");

        public static string ConfigFilePath =>
            Path.Combine(ConfigFolder, "NoiseFilterConfig.json");

        public static NoiseFilterConfig Load()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                    return CreateDefault();

                var json = File.ReadAllText(ConfigFilePath);
                var cfg = JsonSerializer.Deserialize<NoiseFilterConfig>(json);

                return cfg ?? CreateDefault();
            }
            catch
            {
                return CreateDefault();
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(ConfigFolder);
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(ConfigFilePath, json);
            }
            catch
            {
                // Falls Speichern fehlschlägt → still
            }
        }

        private static NoiseFilterConfig CreateDefault()
        {
            return new NoiseFilterConfig
            {
                BadWords = new List<string>
                {
                    "test",
                    "debug",
                    "fehler",
                    "xxx",
                    "asdf"
                }
            };
        }
    }
}