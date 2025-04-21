using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System;

namespace Core;

public class Msg
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ChatClient
{
    public async Task<string> SendAsync(List<Msg> messages)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            return "[Error: OPENAI_API_KEY not set]";

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var payload = new
        {
            model = "gpt-4.1-nano-2025-04-14",
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
            // TODO: Add image support if any message includes an image
        };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        try
        {
            var resp = await http.PostAsync("https://api.openai.com/v1/chat/completions", content);
            if (!resp.IsSuccessStatusCode)
                return $"[OpenAI error: {resp.StatusCode}]";
            using var stream = await resp.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var reply = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            return reply ?? "[No reply]";
        }
        catch (Exception ex)
        {
            return $"[Exception: {ex.Message}]";
        }
    }
} 