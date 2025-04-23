using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Voice
{
    public class WhisperService
    {
        private readonly string _apiKey;
        public WhisperService(string? apiKey = null)
        {
            _apiKey = apiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OPENAI_API_KEY not set");
        }

        public async Task<string> TranscribeAsync(string wavFilePath)
        {
            if (!File.Exists(wavFilePath))
            {
                throw new FileNotFoundException($"WAV file not found: {wavFilePath}");
            }
            var fileInfo = new FileInfo(wavFilePath);
            if (fileInfo.Length == 0)
            {
                throw new InvalidOperationException($"WAV file is empty: {wavFilePath}");
            }
            Console.WriteLine($"[Whisper] Transcribing file: {wavFilePath} ({fileInfo.Length} bytes)");

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            using var form = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(wavFilePath);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            form.Add(fileContent, "file", Path.GetFileName(wavFilePath));
            form.Add(new StringContent("whisper-1"), "model");

            HttpResponseMessage response = null!;
            try
            {
                response = await http.PostAsync("https://api.openai.com/v1/audio/transcriptions", form);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException)
            {
                if (response != null)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[Whisper] Error response: {errorBody}");
                }
                throw;
            }
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("text").GetString() ?? string.Empty;
        }
    }
} 