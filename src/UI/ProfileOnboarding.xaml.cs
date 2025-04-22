using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace UI
{
    public partial class ProfileOnboarding : Window
    {
        private int _step = 0;
        private readonly int _totalSteps = 5;
        private readonly Profile _profile;
        private readonly string[] _questions = new[]
        {
            "Hi! I'm Skai. What's your name?",
            "What pronouns do you use? (optional)",
            "What are a few things you love or are interested in?",
            "How do you want me to talk to you? (e.g., playful, serious, super friendly, etc.)",
            "What do you want to get out of chatting with Skai? (optional)"
        };
        private readonly string[] _placeholders = new[]
        {
            "Your name",
            "e.g., he/him, she/her, they/them",
            "e.g., programming, music, sci-fi",
            "e.g., playful, serious, super friendly",
            "e.g., motivation, fun, companionship"
        };
        private TextBox _inputBox;

        public ProfileOnboarding(Profile profile)
        {
            InitializeComponent();
            _profile = profile;
            RenderStep();
        }

        private void RenderStep()
        {
            // Progress dots
            ProgressPanel.Children.Clear();
            for (int i = 0; i < _totalSteps; i++)
            {
                var dot = new Ellipse
                {
                    Width = 12,
                    Height = 12,
                    Margin = new Thickness(4, 0, 4, 0),
                    Fill = i == _step ? new SolidColorBrush(Color.FromRgb(41, 121, 255)) : new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                    Opacity = i == _step ? 1.0 : 0.5
                };
                ProgressPanel.Children.Add(dot);
            }

            // Main content
            ContentPanel.Children.Clear();
            var q = new TextBlock
            {
                Text = _questions[_step],
                Foreground = Brushes.White,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 12),
                TextWrapping = TextWrapping.Wrap
            };
            ContentPanel.Children.Add(q);
            _inputBox = new TextBox
            {
                FontSize = 15,
                MinWidth = 220,
                Height = 32,
                Margin = new Thickness(0, 0, 0, 8),
                Background = new SolidColorBrush(Color.FromRgb(35, 35, 35)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(41, 121, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 0, 8, 0),
                Text = GetCurrentValue()
            };
            ContentPanel.Children.Add(_inputBox);
            BackButton.IsEnabled = _step > 0;
            NextButton.Content = _step == _totalSteps - 1 ? "Finish" : "Next";
        }

        private string GetCurrentValue()
        {
            return _step switch
            {
                0 => _profile.Name,
                1 => _profile.Pronouns,
                2 => _profile.Interests,
                3 => _profile.PreferredTone,
                4 => _profile.Goals,
                _ => ""
            };
        }

        private void SetCurrentValue(string value)
        {
            switch (_step)
            {
                case 0: _profile.Name = value; break;
                case 1: _profile.Pronouns = value; break;
                case 2: _profile.Interests = value; break;
                case 3: _profile.PreferredTone = value; break;
                case 4: _profile.Goals = value; break;
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            SetCurrentValue(_inputBox.Text.Trim());
            if (_step < _totalSteps - 1)
            {
                _step++;
                RenderStep();
            }
            else
            {
                _profile.Save();
                this.DialogResult = true;
                this.Close();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_step > 0)
            {
                SetCurrentValue(_inputBox.Text.Trim());
                _step--;
                RenderStep();
            }
        }
    }
} 