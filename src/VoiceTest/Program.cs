using System;
using System.Threading.Tasks;
using Voice;
using NAudio.Wave;

class Program
{
    static async Task Main(string[] args)
    {
        // Replace with your actual key/region or load from env/config
        string subscriptionKey = "cfJxVsZz1MxgurhmTvBkT2W3zA7vT8TD86pqXuae0iuZK9QRWsiSJQQJ99BDACLArgHXJ3w3AAAYACOGIJlj";
        string subscriptionRegion = "southcentralus";
        var tts = new AzureTTSService(subscriptionKey, subscriptionRegion);
        string text = "This is a test of the AzureTTSService class from the VoiceTest app.";
        string style = "affectionate";
        string outputPath = "output/test_service.wav";
        try
        {
            await tts.SynthesizeToFileAsync(text, style, outputPath);
            Console.WriteLine($"Synthesis complete: {outputPath}");

            // Play the WAV file so the user can hear the assistant's voice
            using (var audioFile = new AudioFileReader(outputPath))
            using (var outputDevice = new WaveOutEvent())
            {
                outputDevice.Init(audioFile);
                outputDevice.Play();
                Console.WriteLine("Playing audio...");
                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    await Task.Delay(100);
                }
            }
            Console.WriteLine("Playback finished.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Synthesis failed: {ex.Message}");
        }
    }
}
