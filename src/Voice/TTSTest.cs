using System;
using System.Threading.Tasks;
using System.IO;

namespace Voice
{
    class TTSTest
    {
        static async Task Main(string[] args)
        {
            // Azure TTS test (disabled)
            /*
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
            // ElevenLabs TTS test (disabled)
            string elevenLabsApiKey = "sk_93023778c72da80ff4f8952861956aeb9f039f0962a82039";
            var elevenTts = new ElevenLabsTTSService(elevenLabsApiKey);
            string elevenText = "This is a test of the ElevenLabsTTSService class.";
            string elevenOutputPath = "output/elevenlabs_test.mp3";
            try
            {
                await elevenTts.SynthesizeToFileAsync(elevenText, elevenOutputPath);
                Console.WriteLine($"ElevenLabs synthesis complete: {elevenOutputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ElevenLabs synthesis failed: {ex.Message}");
            }
            */
            TestVosk();
            TestHotword();
        }

        // Vosk Speech Recognition test
        static void TestVosk()
        {
            string modelPath = "vosk-model"; // Download and extract model to this folder
            string wavFile = "temp.wav"; // Use AudioRecorder to create this file
            try
            {
                var recognizer = new VoskSpeechRecognizer(modelPath);
                var result = recognizer.RecognizeFromWav(wavFile);
                Console.WriteLine($"Vosk recognition result: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Vosk recognition failed: {ex.Message}");
            }
        }

        static void TestHotword()
        {
            string modelPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "vosk-model"));
            Console.WriteLine($"[DEBUG] About to start hotword listener. Model path: {modelPath}");
            try
            {
                VoskHotwordListener.ListenForHotword(modelPath, "computer", () =>
                {
                    Console.WriteLine("Hotword detected! Speak your command now...");
                    var sttResult = VoskSpeechRecognizer.RecognizeFromMicrophone(modelPath, 7);
                    Console.WriteLine($"[Vosk STT] Result: {sttResult}");
                });
                Console.WriteLine("[DEBUG] Hotword listener finished normally.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Exception in hotword listener: {ex}");
            }
        }
    }
} 