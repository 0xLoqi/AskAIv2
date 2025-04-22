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
using System.Text.Json;
using System.IO;

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
        public IntPtr _previousActiveWindow = IntPtr.Zero;
        public string? _lastScreenshotPath;
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

        private bool _isRecording = false;
        private const int AltSpaceKeyCode = 32; // Space
        private bool _altDown = false;
        private bool _spaceDown = false;
        private DateTime _recordStartTime;
        private bool _ctrlDown = false;

        private bool _screenshotZoomed = false;

        private const string MemoryFilePath = "memory.json";
        private static readonly string ChatsDir = "chats";

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
            this.Deactivated += Overlay_Deactivated;
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

            // Do NOT show screenshot preview by default
            HideScreenshotAttachedIndicator();
            ScreenshotPreviewPane.Visibility = Visibility.Collapsed;
            ScreenshotPreviewImage.Source = null;
            _lastScreenshotPath = null;

            this.SizeChanged += (s, ev) => CenterOverlayOnScreen();
            Dispatcher.BeginInvoke(new Action(CenterOverlayOnScreen), DispatcherPriority.ApplicationIdle);

            // Ensure placeholder is set correctly on load
            InputTextBox_TextChanged(InputTextBox, null);
            // Always focus the input box on overlay load
            InputTextBox.Focus();
            InputTextBox.CaretIndex = InputTextBox.Text.Length;
            LoadMessages();
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
            if (e.KeyCode == System.Windows.Forms.Keys.ControlKey || e.KeyCode == System.Windows.Forms.Keys.LControlKey || e.KeyCode == System.Windows.Forms.Keys.RControlKey) _ctrlDown = true;
            if (e.KeyCode == System.Windows.Forms.Keys.Space) _spaceDown = true;
            // Debug: confirm key handler is firing
            System.Diagnostics.Debug.WriteLine($"KeyDown: {e.KeyCode}, Ctrl: {_ctrlDown}, Space: {_spaceDown}");
            if (_ctrlDown && _spaceDown && !_isRecording && this.IsVisible && InputTextBox.IsFocused && this.IsActive)
            {
                _recordStartTime = DateTime.Now;
                DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                timer.Tick += (s, ev) =>
                {
                    timer.Stop();
                    if (_ctrlDown && _spaceDown && !_isRecording && this.IsVisible && InputTextBox.IsFocused && this.IsActive)
                    {
                        StartVoiceRecording();
                    }
                };
                timer.Start();
            }
        }

        private async void GlobalHook_KeyUp(object? sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.ControlKey || e.KeyCode == System.Windows.Forms.Keys.LControlKey || e.KeyCode == System.Windows.Forms.Keys.RControlKey) _ctrlDown = false;
            if (e.KeyCode == System.Windows.Forms.Keys.Space) _spaceDown = false;
            // Debug: confirm key handler is firing
            System.Diagnostics.Debug.WriteLine($"KeyUp: {e.KeyCode}, Ctrl: {_ctrlDown}, Space: {_spaceDown}");
            if (_isRecording && (!_ctrlDown || !_spaceDown))
            {
                await StopVoiceRecordingAsync();
            }
        }

        private void StartVoiceRecording()
        {
            _isRecording = true;
            _lastTempFile = $"temp_{DateTime.Now:yyyyMMdd_HHmmss_fff}.wav";
            _recorder = new AudioRecorder(_lastTempFile);
            _recorder.StartRecording();
            WaveformCanvas.Visibility = Visibility.Visible;
            AnimateWaveform();
        }

        private async Task StopVoiceRecordingAsync()
        {
            _isRecording = false;
            WaveformCanvas.Visibility = Visibility.Collapsed;
            if (_recorder != null)
            {
                _recorder.StopRecording();
                await _recorder.RecordingStoppedTask;
                _recorder = null;
                await Task.Delay(100);
                if (_lastTempFile != null)
                {
                    var whisper = new WhisperService();
                    var transcript = await whisper.TranscribeAsync(_lastTempFile);
                    try { System.IO.File.Delete(_lastTempFile); } catch { }
                    if (!string.IsNullOrWhiteSpace(transcript))
                    {
                        if (!string.IsNullOrWhiteSpace(InputTextBox.Text))
                            InputTextBox.Text += " ";
                        InputTextBox.Text += transcript;
                        InputTextBox.Focus();
                        InputTextBox.CaretIndex = InputTextBox.Text.Length;
                    }
                }
            }
        }

        private void AnimateWaveform()
        {
            // Minimal animated waveform: sine wave animation
            var path = this.FindName("WaveformPath") as System.Windows.Shapes.Path;
            if (path == null) return;
            var geo = new System.Windows.Media.PathGeometry();
            var fig = new System.Windows.Media.PathFigure { StartPoint = new System.Windows.Point(0, 12) };
            int points = 32;
            double width = 640, height = 24;
            for (int i = 1; i <= points; i++)
            {
                double x = i * width / points;
                double y = 12 + 8 * Math.Sin(i + DateTime.Now.Millisecond / 100.0);
                fig.Segments.Add(new System.Windows.Media.LineSegment(new System.Windows.Point(x, y), true));
            }
            geo.Figures.Add(fig);
            path.Data = geo;
            var anim = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
            anim.Tick += (s, e) =>
            {
                if (!_isRecording) { anim.Stop(); return; }
                var newGeo = new System.Windows.Media.PathGeometry();
                var newFig = new System.Windows.Media.PathFigure { StartPoint = new System.Windows.Point(0, 12) };
                for (int i = 1; i <= points; i++)
                {
                    double x = i * width / points;
                    double y = 12 + 8 * Math.Sin(i + DateTime.Now.Millisecond / 100.0 + Environment.TickCount / 100.0);
                    newFig.Segments.Add(new System.Windows.Media.LineSegment(new System.Windows.Point(x, y), true));
                }
                newGeo.Figures.Add(newFig);
                path.Data = newGeo;
            };
            anim.Start();
        }

        private async void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Suppress space input if Ctrl+Space is held for voice input
            if (e.Key == Key.Space && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Enter)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    // Allow newline
                    return;
                }
                e.Handled = true; // Prevent newline
                var text = InputTextBox.Text;
                if (!string.IsNullOrWhiteSpace(text) || !string.IsNullOrEmpty(_lastScreenshotPath))
                {
                    // Add user message
                    AddMessageAndScroll(new Message { Role = "user", Text = text, ImagePath = _lastScreenshotPath });
                    _history.Add(new Msg { Role = "user", Content = text });
                    InputTextBox.Clear();

                    // Show loading message
                    var loadingMsg = new Message { Role = "assistant", Text = "⏳ Thinking..." };
                    Messages.Add(loadingMsg);

                    string reply = string.Empty;
                    try
                    {
                        if (!string.IsNullOrEmpty(_lastScreenshotPath))
                        {
                            reply = await VisionClient.AskWithImageAsync(_lastScreenshotPath, text);
                        }
                        else
                        {
                            reply = await _chatClient.SendAsync(_history);
                        }
                    }
                    catch (Exception ex)
                    {
                        reply = $"❌ Error: {ex.Message}";
                    }

                    // Remove loading message and add real reply
                    Messages.Remove(loadingMsg);
                    AddMessageAndScroll(new Message { Role = "assistant", Text = reply });
                    _history.Add(new Msg { Role = "assistant", Content = reply });

                    // Clear screenshot after sending
                    HideScreenshotAttachedIndicator();
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
            // Show the preview pane with the image, not the modal or message box
            ScreenshotPreviewImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(_lastScreenshotPath));
            ScreenshotPreviewPane.Visibility = Visibility.Visible;
        }

        private void ScreenshotModal_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ScreenshotModal.Visibility = Visibility.Collapsed;
            ScreenshotModalImage.Source = null;
        }

        private void RemoveScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            HideScreenshotAttachedIndicator();
            // Do NOT add a system message when screenshot is removed
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
            this.Hide(); // Only hide, do not close
        }

        private void AddMessageAndScroll(Message message)
        {
            Messages.Add(message);
            SaveMessages();
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

        private void ScreenshotPreviewImage_Click(object sender, MouseButtonEventArgs e)
        {
            if (!_screenshotZoomed)
            {
                ScreenshotPreviewImage.MaxWidth = 800;
                ScreenshotPreviewImage.MaxHeight = 440;
                _screenshotZoomed = true;
            }
            else
            {
                ScreenshotPreviewImage.MaxWidth = 400;
                ScreenshotPreviewImage.MaxHeight = 220;
                _screenshotZoomed = false;
            }
        }

        private void RemoveScreenshotPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            ScreenshotPreviewPane.Visibility = Visibility.Collapsed;
            ScreenshotPreviewImage.Source = null;
            _lastScreenshotPath = null;
            // Do NOT add a system message when screenshot is removed
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var text = InputTextBox.Text;
            if (string.IsNullOrWhiteSpace(text) && string.IsNullOrEmpty(_lastScreenshotPath))
                return;

            // Add user message
            AddMessageAndScroll(new Message { Role = "user", Text = text, ImagePath = _lastScreenshotPath });
            _history.Add(new Msg { Role = "user", Content = text });
            InputTextBox.Clear();

            // Show loading message
            var loadingMsg = new Message { Role = "assistant", Text = "⏳ Thinking..." };
            Messages.Add(loadingMsg);

            string reply = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(_lastScreenshotPath))
                {
                    reply = await VisionClient.AskWithImageAsync(_lastScreenshotPath, text);
                }
                else
                {
                    reply = await _chatClient.SendAsync(_history);
                }
            }
            catch (Exception ex)
            {
                reply = $"❌ Error: {ex.Message}";
            }

            // Remove loading message and add real reply
            Messages.Remove(loadingMsg);
            AddMessageAndScroll(new Message { Role = "assistant", Text = reply });
            _history.Add(new Msg { Role = "assistant", Content = reply });

            // Clear screenshot after sending
            HideScreenshotAttachedIndicator();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void PinButton_Checked(object sender, RoutedEventArgs e)
        {
            // Do nothing (stay open)
        }

        private void PinButton_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Hide(); // Auto-dismiss when unpinned
        }

        private void Overlay_Deactivated(object? sender, EventArgs e)
        {
            if (PinButton.IsChecked == false)
            {
                this.Hide();
            }
        }

        private void SaveMessages()
        {
            try
            {
                var json = JsonSerializer.Serialize(Messages);
                File.WriteAllText(MemoryFilePath, json);
            }
            catch { /* Ignore errors for MVP */ }
        }

        private void LoadMessages()
        {
            try
            {
                if (File.Exists(MemoryFilePath))
                {
                    var json = File.ReadAllText(MemoryFilePath);
                    var loaded = JsonSerializer.Deserialize<ObservableCollection<Message>>(json);
                    if (loaded != null)
                    {
                        Messages.Clear();
                        foreach (var msg in loaded)
                            Messages.Add(msg);
                    }
                }
            }
            catch { /* Ignore errors for MVP */ }
        }

        private void NewChatButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(ChatsDir)) Directory.CreateDirectory(ChatsDir);
                if (Messages.Count > 0)
                {
                    var file = Path.Combine(ChatsDir, $"chat-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                    var json = JsonSerializer.Serialize(Messages);
                    File.WriteAllText(file, json);
                }
                Messages.Clear();
                SaveMessages(); // Clear memory.json for new chat
            }
            catch { /* Ignore errors for MVP */ }
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(ChatsDir)) Directory.CreateDirectory(ChatsDir);
                var files = Directory.GetFiles(ChatsDir, "chat-*.json").OrderByDescending(f => f).ToList();
                HistoryPopupPanel.Children.Clear();
                foreach (var file in files)
                {
                    var date = Path.GetFileNameWithoutExtension(file).Replace("chat-", "");
                    int msgCount = 0;
                    try
                    {
                        var json = File.ReadAllText(file);
                        var msgs = JsonSerializer.Deserialize<ObservableCollection<Message>>(json);
                        msgCount = msgs?.Count ?? 0;
                    }
                    catch { }
                    var btn = new Button
                    {
                        Content = $"{date} ({msgCount} msgs)",
                        Tag = file,
                        Margin = new Thickness(0, 0, 0, 2),
                        HorizontalContentAlignment = HorizontalAlignment.Left,
                        Style = (Style)FindResource("IconButtonStyle")
                    };
                    btn.Click += (s, ev) => { LoadChatFile(file); HistoryPopup.IsOpen = false; };
                    HistoryPopupPanel.Children.Add(btn);
                }
                HistoryPopup.IsOpen = true;
            }
            catch { /* Ignore errors for MVP */ }
        }

        private void LoadChatFile(string file)
        {
            try
            {
                var json = File.ReadAllText(file);
                var loaded = JsonSerializer.Deserialize<ObservableCollection<Message>>(json);
                if (loaded != null)
                {
                    Messages.Clear();
                    foreach (var msg in loaded)
                        Messages.Add(msg);
                    SaveMessages(); // Update memory.json
                }
            }
            catch { /* Ignore errors for MVP */ }
        }
    }
} 