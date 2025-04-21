using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace Voice
{
    public class AzureTTSService
    {
        private readonly SpeechConfig _config;
        private readonly string _voiceName;

        public AzureTTSService(string subscriptionKey, string subscriptionRegion, string voiceName = "en-US-SaraNeural")
        {
            _voiceName = voiceName;
            _config = SpeechConfig.FromSubscription(subscriptionKey, subscriptionRegion);
            _config.SpeechSynthesisVoiceName = _voiceName;
            _config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm);
        }

        public async Task SynthesizeToFileAsync(string text, string style, string outputPath)
        {
            string ssml = $@"
<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xmlns:mstts='http://www.w3.org/2001/mstts' xml:lang='en-US'>
  <voice name='{_voiceName}'>
    <mstts:express-as style='{style}' styledegree='1.0'>
      <prosody rate='1.1'>
        {System.Security.SecurityElement.Escape(text)}
      </prosody>
    </mstts:express-as>
  </voice>
</speak>";

            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            using var fileOutput = AudioConfig.FromWavFileOutput(outputPath);
            using var synthesizer = new SpeechSynthesizer(_config, fileOutput);
            var result = await synthesizer.SpeakSsmlAsync(ssml);
            if (result.Reason != ResultReason.SynthesizingAudioCompleted)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                throw new Exception($"Speech synthesis failed: {result.Reason}, {cancellation.ErrorDetails}");
            }
        }
    }
} 