using System;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.IO;

class Program
{
    static async Task Main()
    {
        string subscriptionKey = "cfJxVsZz1MxgurhmTvBkT2W3zA7vT8TD86pqXuae0iuZK9QRWsiSJQQJ99BDACLArgHXJ3w3AAAYACOGIJlj";
        string subscriptionRegion = "southcentralus";
        var config = SpeechConfig.FromSubscription(subscriptionKey, subscriptionRegion);
        var voiceName = "en-US-SaraNeural";
        config.SpeechSynthesisVoiceName = voiceName;
        config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm);

        var samples = new[]
        {
            (text: "I'm always here to help you, no matter what you need.", style: "affectionate", file: "affectionate_1.wav"),
            // (text: "You're doing great—remember, every question is a step forward.", style: "affectionate", file: "affectionate_2.wav"),
            // (text: "Thank you for spending your time with me. I really enjoy our conversations.", style: "affectionate", file: "affectionate_3.wav"),
            // (text: "If you ever feel stuck, just ask. I'm happy to help you figure things out.", style: "affectionate", file: "affectionate_4.wav"),
            // (text: "You matter, and your curiosity makes this a better place.", style: "affectionate", file: "affectionate_5.wav")
        };

        var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "output");
        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

        foreach (var (text, style, file) in samples)
        {
            var outputPath = Path.Combine(outputDir, file);
            string ssml = $@"
<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xmlns:mstts='http://www.w3.org/2001/mstts' xml:lang='en-US'>
  <voice name='{voiceName}'>
    <mstts:express-as style='{style}' styledegree='1.0'>
      <prosody rate='0%'>
        {System.Security.SecurityElement.Escape(text)}
      </prosody>
      <break time='300ms'/>
    </mstts:express-as>
  </voice>
</speak>";

            using var fileOutput = AudioConfig.FromWavFileOutput(outputPath);
            using var synthesizer = new SpeechSynthesizer(config, fileOutput);

            var result = await synthesizer.SpeakSsmlAsync(ssml);
            Console.WriteLine($"Synthesized '{text}' as {style} to {outputPath}");
        }
    }
}