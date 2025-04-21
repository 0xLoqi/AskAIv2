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
using System.Linq;
using NAudio.Wave;
using Vision;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;

namespace UI
{
    public partial class Overlay : OverlayWindow
    {
        public class Message
        {
            public string Role { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
            public string? ImagePath { get; set; } // Optional: path to image for preview
        }

        public ObservableCollection<Message> Messages { get; set; } = new ObservableCollection<Message>
        {
            new Message { Role = "assistant", Text = "Hi, I'm SkAI" },
            new Message { Role = "assistant", Text = "Type below and press Enter." },
        };

        private readonly ChatClient _chatClient = new ChatClient();
        private readonly List<Msg> _history = new List<Msg>();
        private IKeyboardMouseEvents? _globalHook;
        private AudioRecorder? _recorder;
        private Stopwatch? _pttStopwatch;
        private string? _lastTempFile;
        private AzureTTSService? _ttsService;
        private IntPtr _previousActiveWindow = IntPtr.Zero;
        private string? _lastScreenshotPath;
        private double _originalTop = -1;
        private double _originalHeight = -1;
        private double _defaultTop = -1;
        private double _defaultLeft = -1;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public Overlay(IntPtr previousActiveWindow, string? screenshotPath)
        {
            DotEnv.Load();
            string subscriptionKey = "AuXoWdlB6tJmdAJfx4Epl5H18B31WYPuAZfbb5ZtT074WZWqOZzaJQQJ99BDAC1i4TkXJ3w3AAAYACOG5GcA";
            string subscriptionRegion = "centralus";
            _ttsService = new AzureTTSService(subscriptionKey, subscriptionRegion);
            _previousActiveWindow = previousActiveWindow;
            _lastScreenshotPath = screenshotPath;
            InitializeComponent();
            Loaded += Overlay_Loaded;
            Unloaded += Overlay_Unloaded;
            CameraButton.Click += CameraButton_Click;
            InputTextBox.TextChanged += InputTextBox_TextChanged;
        }

        public Overlay(IntPtr previousActiveWindow) : this(previousActiveWindow, null) { }

        public Overlay() : this(IntPtr.Zero, null) { }

        private void Overlay_Loaded(object sender, RoutedEventArgs e)
        {
            _globalHook = Hook.GlobalEvents();
            _globalHook.KeyDown += GlobalHook_KeyDown;
            _globalHook.KeyUp += GlobalHook_KeyUp;

            if (_defaultTop < 0) _defaultTop = this.Top;
            if (_defaultLeft < 0) _defaultLeft = this.Left;

            if (!string.IsNullOrEmpty(_lastScreenshotPath) && System.IO.File.Exists(_lastScreenshotPath))
            {
                ShowScreenshotAttachedIndicator(_lastScreenshotPath);
            }
            else
            {
                HideScreenshotAttachedIndicator();
            }
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
                        // Speak assistant reply using AzureTTSService and NAudio
                        if (_ttsService != null)
                        {
                            string tempWav = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"assistant_{Guid.NewGuid()}.wav");
                            await _ttsService.SynthesizeToFileAsync(reply, "affectionate", tempWav);
                            using (var audioFile = new AudioFileReader(tempWav))
                            using (var outputDevice = new WaveOutEvent())
                            {
                                outputDevice.Init(audioFile);
                                outputDevice.Play();
                                while (outputDevice.PlaybackState == PlaybackState.Playing)
                                {
                                    await Task.Delay(100);
                                }
                            }
                            try { System.IO.File.Delete(tempWav); } catch { }
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

                    // Speak assistant reply using AzureTTSService and NAudio
                    if (_ttsService != null)
                    {
                        string tempWav = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"assistant_{Guid.NewGuid()}.wav");
                        await _ttsService.SynthesizeToFileAsync(reply, "affectionate", tempWav);
                        using (var audioFile = new AudioFileReader(tempWav))
                        using (var outputDevice = new WaveOutEvent())
                        {
                            outputDevice.Init(audioFile);
                            outputDevice.Play();
                            while (outputDevice.PlaybackState == PlaybackState.Playing)
                            {
                                await Task.Delay(100);
                            }
                        }
                        try { System.IO.File.Delete(tempWav); } catch { }
                    }
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

        private async void CameraButton_Click(object sender, RoutedEventArgs e)
        {
            string outputPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"active_window_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            try
            {
                if (_previousActiveWindow != IntPtr.Zero)
                {
                    Vision.ScreenGrabber.CaptureWindowToPng(_previousActiveWindow, outputPath);
                    _lastScreenshotPath = outputPath;
                    ShowScreenshotAttachedIndicator(outputPath);
                    Messages.Add(new Message { Role = "system", Text = $"🧠 Screenshot sent to AI." });
                }
                else
                {
                    Vision.ScreenGrabber.CaptureActiveWindowToPng(outputPath);
                    _lastScreenshotPath = outputPath;
                    ShowScreenshotAttachedIndicator(outputPath);
                    Messages.Add(new Message { Role = "system", Text = $"🧠 Screenshot sent to AI." });
                }
            }
            catch (Exception ex)
            {
                Messages.Add(new Message { Role = "system", Text = $"❌ Screenshot failed: {ex.Message}" });
            }
            await Task.CompletedTask;
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            InputPlaceholder.Visibility = string.IsNullOrEmpty(InputTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ShowScreenshotAttachedIndicator(string imagePath)
        {
            ScreenshotChipBorder.Visibility = Visibility.Visible;
            bool exists = !string.IsNullOrEmpty(imagePath) && System.IO.File.Exists(imagePath);
            var previewBtn = ScreenshotAttachedPanel.Children.OfType<Button>().FirstOrDefault(b => (string?)b.Content == "Preview");
            if (previewBtn != null) previewBtn.IsEnabled = exists;
        }

        private void HideScreenshotAttachedIndicator()
        {
            ScreenshotChipBorder.Visibility = Visibility.Collapsed;
            _lastScreenshotPath = null;
            var previewBtn = ScreenshotAttachedPanel.Children.OfType<Button>().FirstOrDefault(b => (string?)b.Content == "Preview");
            if (previewBtn != null) previewBtn.IsEnabled = false;
        }

        private void PreviewScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_lastScreenshotPath) || !System.IO.File.Exists(_lastScreenshotPath))
            {
                Messages.Add(new Message { Role = "system", Text = "No screenshot to preview." });
                return;
            }
            ScreenshotPreviewImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(_lastScreenshotPath));
            ScreenshotPreviewPanel.Visibility = Visibility.Visible;
            _originalTop = this.Top;
            _originalHeight = this.Height;
        }

        private void ScreenshotPreviewPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ScreenshotPreviewPanel.Visibility = Visibility.Collapsed;
            ScreenshotPreviewImage.Source = null;
            if (_defaultTop > -1) this.Top = _defaultTop;
            if (_defaultLeft > -1) this.Left = _defaultLeft;
        }

        private void RemoveScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            HideScreenshotAttachedIndicator();
            Messages.Add(new Message { Role = "system", Text = "Screenshot removed from context. Only text will be sent." });
        }

        private void SendImageToAI_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string imagePath && !string.IsNullOrEmpty(imagePath))
            {
                Messages.Add(new Message { Role = "system", Text = $"🧠 Image sent to AI: {imagePath}" });
                // TODO: Integrate with VisionClient or AI pipeline
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
} 