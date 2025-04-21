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

        public ObservableCollection<Message> Messages { get; set; } = new ObservableCollection<Message>();

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

        private static readonly string[] Slogans = new[]
        {
            "- Ask Anything, Anywhere, Anytime",
            "- Curious? Just Ask.",
            "- Your AI Copilot, One Shortcut Away",
            "- Stuck? Get Instant Answers.",
            "- Type Less, Know More.",
            "- Unlock Knowledge. Instantly.",
            "- No Limits. Just Ask.",
            "- AI at Your Fingertips.",
            "- Ask. Learn. Repeat.",
            "- The Answer is a Shortcut Away."
        };

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

            // Set a random slogan using FindName to avoid linter error
            var rand = new System.Random();
            var sloganTextBlock = this.FindName("SloganText") as System.Windows.Controls.TextBlock;
            if (sloganTextBlock != null)
                sloganTextBlock.Text = Slogans[rand.Next(Slogans.Length)];

            if (!string.IsNullOrEmpty(_lastScreenshotPath) && System.IO.File.Exists(_lastScreenshotPath))
            {
                ShowScreenshotAttachedIndicator(_lastScreenshotPath);
            }
            else
            {
                HideScreenshotAttachedIndicator();
            }

            this.SizeChanged += (s, ev) => CenterOverlayOnScreen();
            Dispatcher.BeginInvoke(new Action(CenterOverlayOnScreen), DispatcherPriority.ApplicationIdle);

            // Ensure placeholder is set correctly on load
            InputTextBox_TextChanged(InputTextBox, null);
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
                AddMessageAndScroll(new Message { Role = "system", Text = "🎤 Recording... (hold F8)" });
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
                AddMessageAndScroll(new Message { Role = "system", Text = "🛑 Recording stopped." });
                await Task.Delay(100); // Give time for file to be released
                if (heldMs > 500 && _lastTempFile != null)
                {
                    var whisper = new WhisperService();
                    AddMessageAndScroll(new Message { Role = "system", Text = "⏳ Transcribing..." });
                    var transcript = await whisper.TranscribeAsync(_lastTempFile);
                    try { System.IO.File.Delete(_lastTempFile); } catch { }
                    if (!string.IsNullOrWhiteSpace(transcript))
                    {
                        AddMessageAndScroll(new Message { Role = "user", Text = transcript });
                        _history.Add(new Msg { Role = "user", Content = transcript });
                        var reply = await _chatClient.SendAsync(_history);
                        AddMessageAndScroll(new Message { Role = "assistant", Text = reply });
                        _history.Add(new Msg { Role = "assistant", Content = reply });
                        // Speak assistant reply using AzureTTSService and NAudio
                        if (_ttsService != null && IsVoiceReplyEnabled)
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
                        AddMessageAndScroll(new Message { Role = "system", Text = "❌ No speech detected." });
                    }
                }
                else
                {
                    AddMessageAndScroll(new Message { Role = "system", Text = "⌛ Hold F8 longer for voice input." });
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
                    AddMessageAndScroll(new Message { Role = "user", Text = text });
                    _history.Add(new Msg { Role = "user", Content = text });
                    InputTextBox.Clear();
                    var reply = await _chatClient.SendAsync(_history);
                    AddMessageAndScroll(new Message { Role = "assistant", Text = reply });
                    _history.Add(new Msg { Role = "assistant", Content = reply });

                    // Speak assistant reply using AzureTTSService and NAudio
                    if (_ttsService != null && IsVoiceReplyEnabled)
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
                }
                else
                {
                    Vision.ScreenGrabber.CaptureActiveWindowToPng(outputPath);
                    _lastScreenshotPath = outputPath;
                    ShowScreenshotAttachedIndicator(outputPath);
                }
            }
            catch (Exception ex)
            {
                AddMessageAndScroll(new Message { Role = "system", Text = $"❌ Screenshot failed: {ex.Message}" });
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
            ScreenshotChip.Visibility = Visibility.Visible;
            ScreenshotThumbnail.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(imagePath));
        }

        private void HideScreenshotAttachedIndicator()
        {
            ScreenshotChip.Visibility = Visibility.Collapsed;
            ScreenshotThumbnail.Source = null;
            _lastScreenshotPath = null;
        }

        private void ScreenshotThumbnail_Click(object sender, MouseButtonEventArgs e)
        {
            if (string.IsNullOrEmpty(_lastScreenshotPath) || !System.IO.File.Exists(_lastScreenshotPath))
                return;
            ScreenshotModal.Visibility = Visibility.Visible;
            MessageBox.Show(_lastScreenshotPath, "Debug: Screenshot Path");
            try
            {
                ScreenshotModalImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(_lastScreenshotPath));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load screenshot: " + ex.Message);
            }
        }

        private void ScreenshotModal_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ScreenshotModal.Visibility = Visibility.Collapsed;
            ScreenshotModalImage.Source = null;
        }

        private void RemoveScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            HideScreenshotAttachedIndicator();
            AddMessageAndScroll(new Message { Role = "system", Text = "Screenshot removed from context. Only text will be sent." });
        }

        private void SendImageToAI_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string imagePath && !string.IsNullOrEmpty(imagePath))
            {
                AddMessageAndScroll(new Message { Role = "system", Text = $"🧠 Image sent to AI: {imagePath}" });
                // TODO: Integrate with VisionClient or AI pipeline
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AddMessageAndScroll(Message message)
        {
            Messages.Add(message);
            Dispatcher.InvokeAsync(() => {
                MessagesScrollViewer.ScrollToEnd();
                CenterOverlayOnScreen();
            }, DispatcherPriority.Background);
        }

        private void CenterOverlayOnScreen()
        {
            var screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle).WorkingArea;
            this.Left = screen.Left + (screen.Width - this.ActualWidth) / 2;
            this.Top = screen.Top + (screen.Height - this.ActualHeight) / 2;
        }

        // Allow dragging the window by the header bar
        private void HeaderBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        public bool IsVoiceReplyEnabled => VoiceReplyToggle != null && VoiceReplyToggle.IsChecked == true;

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            Dispatcher.BeginInvoke(new Action(CenterOverlayOnScreen), DispatcherPriority.ApplicationIdle);
        }
    }
} 