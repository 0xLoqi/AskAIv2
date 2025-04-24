using Vosk;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Voice
{
    public class VoskSpeechRecognizer
    {
        private readonly Model _model;
        public VoskSpeechRecognizer(string modelPath)
        {
            if (!Directory.Exists(modelPath))
                throw new DirectoryNotFoundException($"Vosk model directory not found: {modelPath}");
            Vosk.Vosk.SetLogLevel(0); // Silence Vosk logs
            _model = new Model(modelPath);
        }

        public string RecognizeFromWav(string wavFilePath)
        {
            if (!File.Exists(wavFilePath))
                throw new FileNotFoundException($"WAV file not found: {wavFilePath}");
            using var waveReader = new NAudio.Wave.WaveFileReader(wavFilePath);
            using var rec = new VoskRecognizer(_model, waveReader.WaveFormat.SampleRate);
            var buffer = new byte[4096];
            int bytesRead;
            while ((bytesRead = waveReader.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (!rec.AcceptWaveform(buffer, bytesRead))
                    continue;
            }
            var result = rec.FinalResult();
            return result;
        }

        // Live microphone transcription: listens for speech and returns the first non-empty result
        public static string RecognizeFromMicrophone(string modelPath, int maxSeconds = 10)
        {
            if (!Directory.Exists(modelPath))
                throw new DirectoryNotFoundException($"Vosk model directory not found: {modelPath}");
            Vosk.Vosk.SetLogLevel(0);
            using var model = new Model(modelPath);
            using var waveIn = new NAudio.Wave.WaveInEvent { WaveFormat = new NAudio.Wave.WaveFormat(16000, 1) };
            using var rec = new VoskRecognizer(model, 16000.0f);
            rec.SetMaxAlternatives(0);
            rec.SetWords(true);
            string? finalResult = null;
            DateTime start = DateTime.Now;
            waveIn.DataAvailable += (s, e) =>
            {
                if (finalResult != null) return;
                if (rec.AcceptWaveform(e.Buffer, e.BytesRecorded))
                {
                    var result = rec.Result();
                    if (!string.IsNullOrWhiteSpace(result) && result.Contains("text"))
                        finalResult = result;
                }
            };
            waveIn.StartRecording();
            while (finalResult == null && (DateTime.Now - start).TotalSeconds < maxSeconds)
            {
                System.Threading.Thread.Sleep(100);
            }
            waveIn.StopRecording();
            return finalResult ?? "";
        }
    }

    public static class VoskHotwordListener
    {
        public static void ListenForHotword(string modelPath, string hotword = "computer", Action onHotwordDetected = null!, Action? onReady = null)
        {
            Console.WriteLine($"[DEBUG] Entered ListenForHotword. modelPath={Path.GetFullPath(modelPath)} hotword={hotword}");
            if (!Directory.Exists(modelPath))
                throw new DirectoryNotFoundException($"Vosk model directory not found: {modelPath}");
            Vosk.Vosk.SetLogLevel(0);
            Console.WriteLine("[DEBUG] Loading Vosk model...");
            using var model = new Model(modelPath);
            Console.WriteLine("[DEBUG] Model loaded. Setting up microphone...");
            using var waveIn = new NAudio.Wave.WaveInEvent { WaveFormat = new NAudio.Wave.WaveFormat(16000, 1) };
            Console.WriteLine("[DEBUG] Microphone set up. Creating recognizer...");
            using var rec = new VoskRecognizer(model, 16000.0f);
            rec.SetMaxAlternatives(0);
            rec.SetWords(true);
            bool detected = false;
            waveIn.DataAvailable += (s, e) =>
            {
                if (detected) return;
                try {
                    if (rec.AcceptWaveform(e.Buffer, e.BytesRecorded))
                    {
                        var result = rec.Result();
                        if (result != null && result.ToLower().Contains(hotword.ToLower()))
                        {
                            detected = true;
                            Console.WriteLine("[DEBUG] Hotword detected in result.");
                            onHotwordDetected?.Invoke();
                            waveIn.StopRecording();
                        }
                    }
                    else
                    {
                        var partial = rec.PartialResult();
                        try
                        {
                            var doc = JsonDocument.Parse(partial);
                            if (doc.RootElement.TryGetProperty("partial", out var p) && !string.IsNullOrWhiteSpace(p.GetString()))
                            {
                                Console.WriteLine($"[DEBUG] Got partial: {partial}");
                            }
                        }
                        catch { /* ignore parse errors */ }
                        if (partial != null && partial.ToLower().Contains(hotword.ToLower()))
                        {
                            detected = true;
                            Console.WriteLine("[DEBUG] Hotword detected in partial.");
                            onHotwordDetected?.Invoke();
                            waveIn.StopRecording();
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"[DEBUG] Exception in DataAvailable: {ex}");
                }
            };
            Console.WriteLine($"[DEBUG] Starting recording. Listening for hotword '{hotword}'...");
            waveIn.StartRecording();
            onReady?.Invoke();
            while (!detected) System.Threading.Thread.Sleep(100);
            Console.WriteLine("[DEBUG] Exiting ListenForHotword.");
        }
    }

    public class VoskStreamingRecognizer
    {
        private readonly string _modelPath;
        private Model? _model;
        private VoskRecognizer? _recognizer;
        private NAudio.Wave.WaveInEvent? _waveIn;
        private bool _isRunning;
        private bool _disposed = false;
        private bool _stopping = false;
        private Action<string>? _onPartial;
        private Action<string>? _onFinal;
        private int _silenceMs;
        private DateTime _lastAudio;
        private volatile bool _cancelled = false;
        private bool _hasSpeech = false;
        private readonly object _recognizerLock = new object();
        private ConcurrentQueue<(byte[] Buffer, int Bytes)> _audioQueue;
        private AutoResetEvent _audioAvailable;
        private Thread _processingThread;
        private bool _processingActive = false;
        private bool _useSilenceDetection = true;

        public VoskStreamingRecognizer(string modelPath, int silenceMs = 4800, bool useSilenceDetection = true)
        {
            _modelPath = modelPath;
            _silenceMs = silenceMs;
            _useSilenceDetection = useSilenceDetection;
            Console.WriteLine($"[VoskStreamingRecognizer] Constructed. useSilenceDetection={_useSilenceDetection}, silenceMs={_silenceMs}");
        }

        public void Start(Action<string> onPartial, Action<string> onFinal, bool? useSilenceDetection = null)
        {
            if (_isRunning) return;
            _disposed = false;
            _stopping = false;
            _cancelled = false;
            _hasSpeech = false;
            _onPartial = onPartial;
            _onFinal = onFinal;
            if (useSilenceDetection.HasValue) _useSilenceDetection = useSilenceDetection.Value;
            Console.WriteLine($"[VoskStreamingRecognizer] Start called. useSilenceDetection={_useSilenceDetection}");
            _model = new Model(_modelPath);
            _recognizer = new VoskRecognizer(_model, 16000.0f);
            _recognizer.SetWords(true);
            _audioQueue = new ConcurrentQueue<(byte[], int)>();
            _audioAvailable = new AutoResetEvent(false);
            _processingActive = true;
            _processingThread = new Thread(ProcessAudioQueue) { IsBackground = true };
            _processingThread.Start();
            _waveIn = new NAudio.Wave.WaveInEvent { WaveFormat = new NAudio.Wave.WaveFormat(16000, 1) };
            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.StartRecording();
            _isRunning = true;
            _lastAudio = DateTime.Now;
            if (_useSilenceDetection)
            {
                Console.WriteLine("[VoskStreamingRecognizer] Starting MonitorSilence (enabled)");
                Task.Run(() => MonitorSilence());
            }
            else
            {
                Console.WriteLine("[VoskStreamingRecognizer] Silence detection is DISABLED");
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _disposed = true;
            _stopping = true;
            _cancelled = true;
            _processingActive = false;
            _audioAvailable?.Set(); // Wake up processing thread
            if (_waveIn != null)
                _waveIn.DataAvailable -= OnDataAvailable;
            Thread.Sleep(100); // Ensure no more DataAvailable events are queued
            _waveIn?.StopRecording();
            _waveIn?.Dispose();
            _processingThread?.Join(500); // Wait for processing thread to exit
            lock (_recognizerLock)
            {
                _recognizer?.Dispose();
                _model?.Dispose();
            }
            _isRunning = false;
        }

        private void OnDataAvailable(object? sender, NAudio.Wave.WaveInEventArgs e)
        {
            // Just enqueue the buffer and signal the processing thread
            if (_disposed || _stopping || _cancelled || !_processingActive) return;
            var buffer = new byte[e.BytesRecorded];
            Array.Copy(e.Buffer, buffer, e.BytesRecorded);
            _audioQueue.Enqueue((buffer, e.BytesRecorded));
            _audioAvailable.Set();
            _lastAudio = DateTime.Now;
        }

        private void ProcessAudioQueue()
        {
            while (_processingActive && !_disposed && !_stopping && !_cancelled)
            {
                _audioAvailable.WaitOne();
                while (_audioQueue.TryDequeue(out var item))
                {
                    lock (_recognizerLock)
                    {
                        if (_disposed || _stopping || _cancelled || _recognizer == null || !_isRunning) return;
                        try
                        {
                            if (_recognizer.AcceptWaveform(item.Buffer, item.Bytes))
                            {
                                if (_cancelled) return;
                                var final = _recognizer.Result();
                                _onFinal?.Invoke(final);
                            }
                            else
                            {
                                if (_cancelled) return;
                                var partial = _recognizer.PartialResult();
                                if (!string.IsNullOrWhiteSpace(partial) && partial.Contains("partial") && !partial.Contains("\"partial\" : \"\""))
                                    _hasSpeech = true;
                                _onPartial?.Invoke(partial);
                            }
                        }
                        catch
                        {
                            // Swallow all exceptions to prevent native crash
                            return;
                        }
                    }
                }
            }
        }

        private void MonitorSilence()
        {
            Console.WriteLine($"[VoskStreamingRecognizer] MonitorSilence running. useSilenceDetection={_useSilenceDetection}");
            if (!_useSilenceDetection) return;
            while (_isRunning && !_disposed && !_stopping && !_cancelled)
            {
                if ((DateTime.Now - _lastAudio).TotalMilliseconds > _silenceMs)
                {
                    if (!_disposed && !_stopping && !_cancelled && _recognizer != null && _hasSpeech)
                    {
                        try
                        {
                            var final = _recognizer.FinalResult();
                            _onFinal?.Invoke(final);
                        }
                        catch (Exception ex)
                        {
                            // Optionally log or swallow
                        }
                    }
                    // Do NOT call Stop() here! Let user action stop the recognizer.
                    break;
                }
                Thread.Sleep(100);
            }
        }
    }
}
