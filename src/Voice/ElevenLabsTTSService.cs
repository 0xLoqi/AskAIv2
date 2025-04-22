using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Voice
{
    public class ElevenLabsTTSService
    {
        private readonly string _apiKey;
        private readonly string _voiceId;
        private readonly string _apiUrl;

        public ElevenLabsTTSService(string apiKey, string voiceId = "Bj9UqZbhQsanLzgalpEG")
        {
            _apiKey = apiKey;
            _voiceId = voiceId;
            _apiUrl = $"https://api.elevenlabs.io/v1/text-to-speech/{_voiceId}";
        }

        public async Task SynthesizeToFileAsync(string text, string outputPath)
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("xi-api-key", _apiKey);
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/mpeg"));

            var payload = new
            {
                text = text,
                model_id = "eleven_monolingual_v1",
                voice_settings = new { stability = 0.5, similarity_boost = 0.75 }
            };
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await http.PostAsync(_apiUrl, content);
            response.EnsureSuccessStatusCode();
            var audioBytes = await response.Content.ReadAsByteArrayAsync();

            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);
            await File.WriteAllBytesAsync(outputPath, audioBytes);
        }
    }
} 