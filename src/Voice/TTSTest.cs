using System;
using System.Threading.Tasks;

namespace Voice
{
    class TTSTest
    {
        static async Task Main(string[] args)
        {
            // Replace with your actual key/region or load from env/config
            string subscriptionKey = "cfJxVsZz1MxgurhmTvBkT2W3zA7vT8TD86pqXuae0iuZK9QRWsiSJQQJ99BDACLArgHXJ3w3AAAYACOGIJlj";
            string subscriptionRegion = "southcentralus";
            var tts = new AzureTTSService(subscriptionKey, subscriptionRegion);
            string text = "This is a test of the AzureTTSService class.";
            string style = "affectionate";
            string outputPath = "output/test_service.wav";
            try
            {
                await tts.SynthesizeToFileAsync(text, style, outputPath);
                Console.WriteLine($"Synthesis complete: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Synthesis failed: {ex.Message}");
            }
        }
    }
} 