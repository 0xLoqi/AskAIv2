using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Core;
using System.Windows.Controls;
using System.Windows.Media;

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
            new Message { Role = "assistant", Text = "Hello! This is a hardcoded message." },
            new Message { Role = "assistant", Text = "Welcome to SkaAI." },
            new Message { Role = "assistant", Text = "Type below and press Enter." },
            new Message { Role = "assistant", Text = "**This should be bold.** _This should be italic._" }
        };

        private readonly ChatClient _chatClient = new ChatClient();
        private readonly List<Msg> _history = new List<Msg>();

        public Overlay() => InitializeComponent();

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