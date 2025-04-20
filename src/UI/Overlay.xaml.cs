using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace UI
{
    public partial class Overlay : OverlayWindow
    {
        public class Message
        {
            public string Text { get; set; } = string.Empty;
        }

        public ObservableCollection<Message> Messages { get; set; } = new ObservableCollection<Message>
        {
            new Message { Text = "Hello! This is a hardcoded message." },
            new Message { Text = "Welcome to SkaAI." },
            new Message { Text = "Type below and press Enter." },
            new Message { Text = "**This should be bold.** _This should be italic._" }
        };

        public Overlay() => InitializeComponent();

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var text = InputTextBox.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    Console.WriteLine($"User typed: {text}");
                    InputTextBox.Clear();
                }
            }
        }
    }
} 