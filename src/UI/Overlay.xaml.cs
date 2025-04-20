using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Core;
using System.Windows.Controls;
using System.Windows.Media;
using Gma.System.MouseKeyHook;
using Voice;
using System.Diagnostics;
using dotenv.net;
using System.Threading.Tasks;
using System.Speech.Synthesis;

namespace UI
{
    public partial class Overlay : OverlayWindow
    {
        public class Message
        {
            public string Role { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
        }

        public ObservableCollection<Message> Messages { get; set; } = new ObservableCollection<Message>
        {
            new Message { Role = "assistant", Text = "Hey. I'm SkAI" },
            new Message { Role = "assistant", Text = "Type below and press Enter." },
        };

        private readonly ChatClient _chatClient = new ChatClient();
        private readonly List<Msg> _history = new List<Msg>();
        private IKeyboardMouseEvents? _globalHook;
        private AudioRecorder? _recorder;
        private Stopwatch? _pttStopwatch;
        private string? _lastTempFile;

        public Overlay()
        {
            DotEnv.Load(); // Ensure .env is loaded for API keys
            Console.WriteLine("Current Directory: " + Environment.CurrentDirectory);
            Console.WriteLine("OPENAI_API_KEY after DotEnv.Load: " + Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
            InitializeComponent();
            Loaded += Overlay_Loaded;
            Unloaded += Overlay_Unloaded;
        }

        private void Overlay_Loaded(object sender, RoutedEventArgs e)
        {
            _globalHook = Hook.GlobalEvents();
            _globalHook.KeyDown += GlobalHook_KeyDown;
            _globalHook.KeyUp += GlobalHook_KeyUp;
        }

        private void Overlay_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_globalHook != null)
            {
                _globalHook.KeyDown -= GlobalHook_KeyDown;
                _globalHook.KeyUp -= GlobalHook_KeyUp;
                _globalHook.Dispose();
                _globalHook = null;
            }
        }

        private void GlobalHook_KeyDown(object? sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.F8 && _recorder == null)
            {
                _lastTempFile = $"temp_{DateTime.Now:yyyyMMdd_HHmmss_fff}.wav";
                _recorder = new AudioRecorder(_lastTempFile);
                _pttStopwatch = Stopwatch.StartNew();
                _recorder.StartRecording();
                Messages.Add(new Message { Role = "system", Text = "🎤 Recording... (hold F8)" });
            }
        }

        private async void GlobalHook_KeyUp(object? sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.F8 && _recorder != null && _pttStopwatch != null)
            {
                _recorder.StopRecording();
                await _recorder.RecordingStoppedTask;
                var heldMs = _pttStopwatch.ElapsedMilliseconds;
                _recorder = null;
                _pttStopwatch = null;
                Messages.Add(new Message { Role = "system", Text = "🛑 Recording stopped." });
                await Task.Delay(100); // Give time for file to be released
                if (heldMs > 500 && _lastTempFile != null)
                {
                    var whisper = new WhisperService();
                    Messages.Add(new Message { Role = "system", Text = "⏳ Transcribing..." });
                    var transcript = await whisper.TranscribeAsync(_lastTempFile);
                    try { System.IO.File.Delete(_lastTempFile); } catch { }
                    if (!string.IsNullOrWhiteSpace(transcript))
                    {
                        Messages.Add(new Message { Role = "user", Text = transcript });
                        _history.Add(new Msg { Role = "user", Content = transcript });
                        var reply = await _chatClient.SendAsync(_history);
                        Messages.Add(new Message { Role = "assistant", Text = reply });
                        _history.Add(new Msg { Role = "assistant", Content = reply });
                        ChatScrollViewer.ScrollToEnd();
                        // Speak assistant reply
                        using (var synth = new SpeechSynthesizer())
                        {
                            synth.SpeakAsync(reply);
                        }
                    }
                    else
                    {
                        Messages.Add(new Message { Role = "system", Text = "❌ No speech detected." });
                    }
                }
                else
                {
                    Messages.Add(new Message { Role = "system", Text = "⌛ Hold F8 longer for voice input." });
                }
            }
        }

        private async void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var text = InputTextBox.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    Messages.Add(new Message { Role = "user", Text = text });
                    _history.Add(new Msg { Role = "user", Content = text });
                    InputTextBox.Clear();
                    var reply = await _chatClient.SendAsync(_history);
                    Messages.Add(new Message { Role = "assistant", Text = reply });
                    _history.Add(new Msg { Role = "assistant", Content = reply });

                    // Auto-scroll to bottom
                    ChatScrollViewer.ScrollToEnd();
                }
            }
        }

        private void OverlayBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
} 