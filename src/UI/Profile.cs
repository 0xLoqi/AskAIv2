using System.Text.Json;
using System.IO;

namespace UI
{
    public class Profile
    {
        public string Name { get; set; } = "";
        public string Pronouns { get; set; } = "";
        public string Interests { get; set; } = "";
        public string PreferredTone { get; set; } = "";
        public string Goals { get; set; } = "";

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