using System.Text.Json;
using System.IO;

namespace UI
{
    public enum VoiceProvider { Basic, Pro }
    public enum VoskModelSize { Small, Large }
    public class Profile
    {
        public string Name { get; set; } = "";
        public string Pronouns { get; set; } = "";
        public string Interests { get; set; } = "";
        public string PreferredTone { get; set; } = "";
        public string Goals { get; set; } = "";
        public VoiceProvider VoiceProvider { get; set; } = VoiceProvider.Basic;
        public string OverlayHotkey { get; set; } = "Alt+Space";
        public VoskModelSize VoskModel { get; set; } = VoskModelSize.Small;

        public static string ProfilePath => "profile.json";

        public static Profile Load()
        {
            if (File.Exists(ProfilePath))
            {
                var json = File.ReadAllText(ProfilePath);
                return JsonSerializer.Deserialize<Profile>(json) ?? new Profile();
            }
            return new Profile();
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ProfilePath, json);
        }
    }
} 