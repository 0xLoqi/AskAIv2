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
using System.Drawing;
using UI;
using System.ComponentModel;

namespace UI
{
    public partial class Overlay : OverlayWindow
    {
        public class Message : INotifyPropertyChanged
        {
            public string Role { get; set; } = string.Empty;
            private string _text = string.Empty;
            public string Text { get => _text; set { _text = value; OnPropertyChanged(nameof(Text)); } }
            private string? _imagePath;
            public string? ImagePath { get => _imagePath; set { _imagePath = value; OnPropertyChanged(nameof(ImagePath)); } }
            public DateTime Timestamp { get; set; } = DateTime.Now;
            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ObservableCollection<Message> Messages { get; set; } = new ObservableCollection<Message>();

        private readonly ChatClient _chatClient = new ChatClient();
        private readonly List<Msg> _history = new List<Msg>();
        private IKeyboardMouseEvents? _globalHook;
        private AudioRecorder? _recorder;
        private Stopwatch? _pttStopwatch;
        private string? _lastTempFile;
        private AzureTTSService? _ttsService;
        private ElevenLabsTTSService? _elevenLabsTTSService;
        private string _elevenLabsApiKey = "sk_93023778c72da80ff4f8952861956aeb9f039f0962a82039";
        public IntPtr _previousActiveWindow = IntPtr.Zero;
        public string? _lastScreenshotPath;
        private double _originalTop = -1;
        private double _originalHeight = -1;
        private double _defaultTop = -1;
        private double _defaultLeft = -1;
        private DateTime _lastMessageTime = DateTime.MinValue;
        private Profile _profile;

        private static readonly string[] Slogans = new[]
        {
            "Ask Anything, Anywhere, Anytime",
            "Curious? Just Ask.",
            "Your AI Copilot, One Shortcut Away",
            "Stuck? Get Instant Answers.",
            "Type Less, Know More.",
            "Unlock Knowledge. Instantly.",
            "No Limits. Just Ask.",
            "AI at Your Fingertips.",
            "Ask. Learn. Repeat.",
            "The Answer is a Shortcut Away."
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

        private class ChatFile
        {
            public string Title { get; set; } = "Untitled Chat";
            public string Snippet { get; set; } = "";
            public string Date { get; set; } = DateTime.Now.ToString("s");
            public List<Message> Messages { get; set; } = new List<Message>();
        }

        public Overlay(IntPtr previousActiveWindow, string? screenshotPath)
        {
            DotEnv.Load();
            string subscriptionKey = "AuXoWdlB6tJmdAJfx4Epl5H18B31WYPuAZfbb5ZtT074WZWqOZzaJQQJ99BDAC1i4TkXJ3w3AAAYACOG5GcA";
            string subscriptionRegion = "centralus";
            _ttsService = new AzureTTSService(subscriptionKey, subscriptionRegion);
            _elevenLabsTTSService = new ElevenLabsTTSService(_elevenLabsApiKey);
            _previousActiveWindow = previousActiveWindow;
            _lastScreenshotPath = null; // Always start with no screenshot attached
            _profile = Profile.Load();
            if (string.IsNullOrWhiteSpace(_profile.Name))
            {
                var wizard = new ProfileOnboarding(_profile);
                wizard.Owner = Application.Current.MainWindow;
                wizard.ShowDialog();
                _profile = Profile.Load(); // Reload in case user updated
            }
            InitializeComponent();
            Loaded += Overlay_Loaded;
            Unloaded += Overlay_Unloaded;
            CameraButton.Click += CameraButton_Click;
            InputTextBox.TextChanged += InputTextBox_TextChanged;
            this.Deactivated += Overlay_Deactivated;
            // Remove: TestSoundButton.Click += TestSoundButton_Click;
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
            _lastScreenshotPath = null;
            ScreenshotPreviewPane.Visibility = Visibility.Collapsed;
            ScreenshotPreviewImage.Source = null;

            this.SizeChanged += (s, ev) => CenterOverlayOnScreen();
            Dispatcher.BeginInvoke(new Action(CenterOverlayOnScreen), DispatcherPriority.ApplicationIdle);

            // Ensure placeholder is set correctly on load
            InputTextBox_TextChanged(InputTextBox, null);
            // Always focus the input box on overlay load
            InputTextBox.Focus();
            InputTextBox.CaretIndex = InputTextBox.Text.Length;
            LoadMessages();
            if (Messages.Count == 0)
            {
                Messages.Clear();
                SaveMessages();
            }

            // If last message was over a minute ago, start new chat
            if (_lastMessageTime != DateTime.MinValue && (DateTime.Now - _lastMessageTime).TotalSeconds > 60)
            {
                Messages.Clear();
                SaveMessages();
            }

            // Set MaxHeight to 80% of the screen's working area
            var screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            this.MaxHeight = screen.WorkingArea.Height * 0.8;
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
            HideScreenshotAttachedIndicator();
            ScreenshotPreviewPane.Visibility = Visibility.Collapsed;
            ScreenshotPreviewImage.Source = null;
        }

        private void GlobalHook_KeyDown(object? sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.ControlKey || e.KeyCode == System.Windows.Forms.Keys.LControlKey || e.KeyCode == System.Windows.Forms.Keys.RControlKey) _ctrlDown = true;
            if (e.KeyCode == System.Windows.Forms.Keys.Space) _spaceDown = true;
            // Debug: confirm key handler is firing
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
                    string imageToSend = _lastScreenshotPath;
                    if (!string.IsNullOrEmpty(_lastScreenshotPath))
                    {
                        string resizedPath = Path.Combine(Path.GetTempPath(), $"resized_{Path.GetFileName(_lastScreenshotPath)}");
                        ResizeImageToMaxDimension(_lastScreenshotPath, resizedPath, 768);
                        imageToSend = resizedPath;
                    }
                    // Add user message
                    AddMessageAndScroll(new Message { Role = "user", Text = text, ImagePath = _lastScreenshotPath });
                    _history.Add(new Msg { Role = "user", Content = text });
                    InputTextBox.Clear();

                    // Clear screenshot after sending (move this up)
                    HideScreenshotAttachedIndicator();
                    ScreenshotPreviewPane.Visibility = Visibility.Collapsed;
                    ScreenshotPreviewImage.Source = null;
                    ScreenshotModal.Visibility = Visibility.Collapsed;
                    ScreenshotModalImage.Source = null;
                    _lastScreenshotPath = null;

                    // Ensure system prompt is first in _history
                    if (_history.Count == 0 || _history[0].Role != "system")
                    {
                        string profileSummary = $"The user you are talking to is named {_profile.Name}. Interests: {_profile.Interests}. Preferred tone: {_profile.PreferredTone}. Your job is to be their favorite companion and make them obsessed with chatting with you.";
                        _history.Insert(0, new Msg { Role = "system", Content = profileSummary + "\n" + Core.PromptTemplate.DefaultSystemPrompt });
                    }

                    // Show loading message
                    var loadingMsg = new Message { Role = "assistant", Text = "typing-indicator" };
                    Messages.Add(loadingMsg);

                    string reply = string.Empty;
                    try
                    {
                        if (!string.IsNullOrEmpty(imageToSend))
                        {
                            reply = await VisionClient.AskWithImageAsync(imageToSend, text);
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
                    var assistantMsg = new Message { Role = "assistant", Text = string.Empty };
                    AddMessageAndScroll(assistantMsg);
                    await StreamInAssistantMessage(reply);
                    _history.Add(new Msg { Role = "assistant", Content = reply });

                    // Voice reply if enabled
                    if (IsVoiceReplyEnabled && !string.IsNullOrWhiteSpace(reply))
                    {
                        string ext = IsUsingElevenLabsTTS ? ".mp3" : ".wav";
                        string audioPath = Path.Combine(Path.GetTempPath(), $"assistant_reply{ext}");
                        await SpeakTextAsync(reply, audioPath);
                        await PlayAudioFileAsync(audioPath);
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
                this.Visibility = Visibility.Collapsed;
                await Task.Delay(150); // Give time for the overlay to hide
                try
                {
                    if (_previousActiveWindow != IntPtr.Zero)
                    {
                        Vision.ScreenGrabber.CaptureWindowToPng(_previousActiveWindow, outputPath);
                        _lastScreenshotPath = outputPath;
                        ScreenshotPreviewImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(outputPath));
                        ScreenshotPreviewPane.Visibility = Visibility.Visible;

                        ScreenshotPreviewPane.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        Vision.ScreenGrabber.CaptureActiveWindowToPng(outputPath);
                        _lastScreenshotPath = outputPath;
                        ScreenshotPreviewImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(outputPath));
                        ScreenshotPreviewPane.Visibility = Visibility.Visible;

                        ScreenshotPreviewPane.Visibility = Visibility.Visible;
                    }
                }
                finally
                {
                    this.Visibility = Visibility.Visible;
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
            // Also close the modal if open
            ScreenshotModal.Visibility = Visibility.Collapsed;
            ScreenshotModalImage.Source = null;
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
            message.Timestamp = DateTime.Now;
            Messages.Add(message);
            _lastMessageTime = DateTime.Now;
            SaveMessages();
            Dispatcher.InvokeAsync(() => {
                MessagesScrollViewer.ScrollToEnd();
                CenterOverlayOnScreen();
            }, DispatcherPriority.Background);
        }

        private void CenterOverlayOnScreen()
        {
            System.Windows.Forms.Screen screen;
            if (_previousActiveWindow != IntPtr.Zero)
            {
                screen = System.Windows.Forms.Screen.FromHandle(_previousActiveWindow);
            }
            else
            {
                screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            }
            this.Left = screen.WorkingArea.Left + (screen.WorkingArea.Width - this.ActualWidth) / 2;
            this.Top = screen.WorkingArea.Top + (screen.WorkingArea.Height - this.ActualHeight) / 2;
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
            CenterOverlayOnScreen();
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
            // Only close the modal, do not remove the screenshot attachment
            ScreenshotModal.Visibility = Visibility.Collapsed;
            ScreenshotModalImage.Source = null;
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var text = InputTextBox.Text;
            if (string.IsNullOrWhiteSpace(text) && string.IsNullOrEmpty(_lastScreenshotPath))
                return;

            string imageToSend = _lastScreenshotPath;
            if (!string.IsNullOrEmpty(_lastScreenshotPath))
            {
                string resizedPath = Path.Combine(Path.GetTempPath(), $"resized_{Path.GetFileName(_lastScreenshotPath)}");
                ResizeImageToMaxDimension(_lastScreenshotPath, resizedPath, 768);
                imageToSend = resizedPath;
            }

            // Add user message
            AddMessageAndScroll(new Message { Role = "user", Text = text, ImagePath = _lastScreenshotPath });
            _history.Add(new Msg { Role = "user", Content = text });
            InputTextBox.Clear();

            // Clear screenshot after sending (move this up)
            HideScreenshotAttachedIndicator();
            ScreenshotPreviewPane.Visibility = Visibility.Collapsed;
            ScreenshotPreviewImage.Source = null;
            ScreenshotModal.Visibility = Visibility.Collapsed;
            ScreenshotModalImage.Source = null;
            _lastScreenshotPath = null;

            // Ensure system prompt is first in _history
            if (_history.Count == 0 || _history[0].Role != "system")
            {
                string profileSummary = $"The user you are talking to is named {_profile.Name}. Interests: {_profile.Interests}. Preferred tone: {_profile.PreferredTone}. Your job is to be their favorite companion and make them obsessed with chatting with you.";
                _history.Insert(0, new Msg { Role = "system", Content = profileSummary + "\n" + Core.PromptTemplate.DefaultSystemPrompt });
            }

            // Show loading message
            var loadingMsg = new Message { Role = "assistant", Text = "typing-indicator" };
            Messages.Add(loadingMsg);

            string reply = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(imageToSend))
                {
                    reply = await VisionClient.AskWithImageAsync(imageToSend, text);
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
            var assistantMsg2 = new Message { Role = "assistant", Text = string.Empty };
            AddMessageAndScroll(assistantMsg2);
            await StreamInAssistantMessage(reply);
            _history.Add(new Msg { Role = "assistant", Content = reply });

            // Voice reply if enabled
            if (IsVoiceReplyEnabled && !string.IsNullOrWhiteSpace(reply))
            {
                string ext = IsUsingElevenLabsTTS ? ".mp3" : ".wav";
                string audioPath = Path.Combine(Path.GetTempPath(), $"assistant_reply{ext}");
                await SpeakTextAsync(reply, audioPath);
                await PlayAudioFileAsync(audioPath);
            }
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
            HideScreenshotAttachedIndicator();
            ScreenshotPreviewPane.Visibility = Visibility.Collapsed;
            ScreenshotPreviewImage.Source = null;
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
                        _lastMessageTime = Messages.Count > 0 ? Messages.Max(m => m.Timestamp) : DateTime.MinValue;
                    }
                }
            }
            catch { /* Ignore errors for MVP */ }
        }

        private async Task<string> GenerateChatTitleAsync(List<Message> messages)
        {
            var prompt = "Summarize this chat in a short, descriptive title (max 8 words).";
            var chatMsgs = new List<Msg> { new Msg { Role = "system", Content = prompt } };
            foreach (var m in messages)
                chatMsgs.Add(new Msg { Role = m.Role, Content = m.Text });
            var title = await _chatClient.SendAsync(chatMsgs);
            if (!string.IsNullOrWhiteSpace(title))
                title = title.Replace("\n", " ").Trim('"');
            return string.IsNullOrWhiteSpace(title) ? "Untitled Chat" : title;
        }

        private async void NewChatButton_Click(object sender, RoutedEventArgs e)
        {
            _profile = Profile.Load(); // Reload profile to get latest changes
            try
            {
                if (!Directory.Exists(ChatsDir)) Directory.CreateDirectory(ChatsDir);
                if (Messages.Count > 0)
                {
                    var file = Path.Combine(ChatsDir, $"chat-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                    var title = await GenerateChatTitleAsync(Messages.ToList());
                    var snippet = Messages.FirstOrDefault(m => m.Role == "user")?.Text ?? "";
                    var chatFile = new ChatFile
                    {
                        Title = title,
                        Snippet = snippet,
                        Date = DateTime.Now.ToString("s"),
                        Messages = Messages.ToList()
                    };
                    var json = JsonSerializer.Serialize(chatFile, new JsonSerializerOptions { WriteIndented = true });
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
                    string title = "Untitled Chat", snippet = "", date = "", msgCount = "";
                    try
                    {
                        var json = File.ReadAllText(file);
                        var chat = JsonSerializer.Deserialize<ChatFile>(json);
                        if (chat == null || chat.Messages == null || chat.Messages.Count == 0)
                            continue; // Skip empty chats
                        title = chat.Title;
                        snippet = chat.Snippet;
                        date = chat.Date;
                        msgCount = chat.Messages.Count.ToString();
                        // Fallback for empty/generic titles
                        if (string.IsNullOrWhiteSpace(title) || title.ToLower().Contains("untitled"))
                        {
                            var firstUser = chat.Messages.FirstOrDefault(m => m.Role == "user")?.Text;
                            title = !string.IsNullOrWhiteSpace(firstUser) ? firstUser : $"Chat on {date}";
                        }
                    }
                    catch { continue; }
                    var panel = new Grid { Margin = new Thickness(0,0,0,6) };
                    panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    var titleBlock = new TextBlock {
                        Text = title,
                        FontWeight = FontWeights.Bold,
                        Foreground = System.Windows.Media.Brushes.White,
                        FontSize = 15,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        MaxWidth = 160,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    Grid.SetColumn(titleBlock, 0);
                    panel.Children.Add(titleBlock);
                    var badge = new Border { Background = System.Windows.Media.Brushes.DimGray, CornerRadius = new CornerRadius(8), Padding = new Thickness(6,0,6,0), Margin = new Thickness(8,0,0,0), VerticalAlignment = VerticalAlignment.Center, Child = new TextBlock { Text = msgCount, Foreground = System.Windows.Media.Brushes.White, FontSize = 12 } };
                    Grid.SetColumn(badge, 1);
                    panel.Children.Add(badge);
                    var dateBlock = new TextBlock { Text = date, Foreground = System.Windows.Media.Brushes.Gray, FontSize = 12, Margin = new Thickness(0,2,0,0) };
                    var snippetBlock = new TextBlock { Text = snippet, Foreground = System.Windows.Media.Brushes.LightGray, FontSize = 13, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 220, Margin = new Thickness(0,2,0,0) };
                    var stack = new StackPanel { Orientation = Orientation.Vertical };
                    stack.Children.Add(panel);
                    stack.Children.Add(dateBlock);
                    if (!string.IsNullOrWhiteSpace(snippet))
                        stack.Children.Add(snippetBlock);
                    var border = new Border { Child = stack, Background = System.Windows.Media.Brushes.Transparent, CornerRadius = new CornerRadius(6), Padding = new Thickness(6), Cursor = System.Windows.Input.Cursors.Hand };
                    border.MouseLeftButtonUp += (s, ev) => { LoadChatFile(file); HistoryPopup.IsOpen = false; };
                    border.MouseEnter += (s, ev) => border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 60, 80));
                    border.MouseLeave += (s, ev) => border.Background = System.Windows.Media.Brushes.Transparent;
                    HistoryPopupPanel.Children.Add(border);
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
                var chat = JsonSerializer.Deserialize<ChatFile>(json);
                if (chat != null && chat.Messages != null)
                {
                    Messages.Clear();
                    foreach (var msg in chat.Messages)
                        Messages.Add(msg);
                    SaveMessages(); // Update memory.json
                }
            }
            catch { /* Ignore errors for MVP */ }
        }

        private void MessagesScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (MessagesScrollViewer != null)
            {
                MessagesScrollViewer.ScrollToVerticalOffset(MessagesScrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }

        public bool IsUsingElevenLabsTTS => TTSProviderToggle != null && TTSProviderToggle.IsChecked == true;

        public async Task SpeakTextAsync(string text, string outputPath)
        {
            try
            {
                if (IsUsingElevenLabsTTS && _elevenLabsTTSService != null)
                {
                    try
                    {
                        await _elevenLabsTTSService.SynthesizeToFileAsync(text, outputPath);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"ElevenLabs TTS failed: {ex.Message}\nFalling back to Azure TTS.", "TTS Error");
                        if (_ttsService != null)
                        {
                            await _ttsService.SynthesizeToFileAsync(text, "general", outputPath);
                        }
                    }
                }
                else if (_ttsService != null)
                {
                    await _ttsService.SynthesizeToFileAsync(text, "general", outputPath);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"TTS synthesis failed: {ex.Message}", "TTS Error");
                return;
            }
            if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
            {
                System.Windows.MessageBox.Show($"TTS did not produce a valid audio file: {outputPath}", "TTS Error");
            }
        }

        public async Task PlayAudioFileAsync(string filePath)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        System.Windows.MessageBox.Show($"Audio file not found: {filePath}", "Audio Playback Error");
                        return;
                    }
                    var fi = new FileInfo(filePath);
                    if (fi.Length == 0)
                    {
                        System.Windows.MessageBox.Show($"Audio file is empty: {filePath}", "Audio Playback Error");
                        return;
                    }
                    using var audioFile = new NAudio.Wave.AudioFileReader(filePath);
                    using var outputDevice = new NAudio.Wave.WaveOutEvent();
                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                    while (outputDevice.PlaybackState == NAudio.Wave.PlaybackState.Playing)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Audio playback failed: {ex.Message}", "Audio Playback Error");
                }
            });
        }

        // Utility: Resize image to max dimension (width or height)
        public static void ResizeImageToMaxDimension(string inputPath, string outputPath, int maxDim)
        {
            using (var src = System.Drawing.Image.FromFile(inputPath))
            {
                int w = src.Width, h = src.Height;
                if (w <= maxDim && h <= maxDim)
                {
                    src.Save(outputPath); // No resize needed
                    return;
                }
                double scale = (double)maxDim / Math.Max(w, h);
                int newW = (int)(w * scale);
                int newH = (int)(h * scale);
                using (var bmp = new System.Drawing.Bitmap(src, newW, newH))
                {
                    bmp.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
        }

        // Add this handler for the settings button
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new SettingsWindow(_profile);
            win.Owner = this;
            win.ShowDialog();
            // Reload profile in case it was changed
            _profile = Profile.Load();
            // If not pinned and not visible, bring overlay back to foreground
            if (PinButton.IsChecked != true && !this.IsVisible)
            {
                this.Show();
                this.Activate();
            }
        }

        // Handler for Pop Out button in screenshot chip
        private void PopOutScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_lastScreenshotPath) && System.IO.File.Exists(_lastScreenshotPath))
            {
                ScreenshotModalImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(_lastScreenshotPath));
                ScreenshotModal.Visibility = Visibility.Visible;
            }
        }

        // Helper: Stream in assistant message text (typewriter effect)
        private async Task StreamInAssistantMessage(string fullText)
        {
            if (Messages.Count == 0) return;
            var msg = Messages.LastOrDefault(m => m.Role == "assistant");
            if (msg == null) return;
            msg.Text = string.Empty;
            for (int i = 0; i < fullText.Length; i++)
            {
                msg.Text += fullText[i];
                // Force UI update
                Messages[Messages.Count - 1] = msg;
                await Task.Delay(12); // ~80 chars/sec
            }
        }

        // Add MessageImage_Click handler
        private void MessageImage_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Image img && img.DataContext is Message msg && !string.IsNullOrEmpty(msg.ImagePath) && System.IO.File.Exists(msg.ImagePath))
            {
                ScreenshotModalImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(msg.ImagePath));
                ScreenshotModal.Visibility = Visibility.Visible;
            }
        }
    }
} 