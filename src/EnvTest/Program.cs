using Core;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // Test .env loader
        Class1.TestEnvLoader();

        // Test ChatClient
        var client = new ChatClient();
        var messages = new List<Msg> { new Msg { Role = "user", Content = "Hello!" } };
        var reply = await client.SendAsync(messages);
        System.Console.WriteLine($"ChatClient reply: {reply}");

        // Test AudioRecorder
        var recorder = new Voice.AudioRecorder();
        System.Console.WriteLine("Recording for 5 seconds...");
        recorder.StartRecording();
        await Task.Delay(5000);
        recorder.StopRecording();
        System.Console.WriteLine("Recording stopped. Check temp.wav.");

        // Test WhisperService
        var whisper = new Voice.WhisperService();
        var transcript = await whisper.TranscribeAsync("temp.wav");
        System.Console.WriteLine($"Transcript: {transcript}");
    }
}
