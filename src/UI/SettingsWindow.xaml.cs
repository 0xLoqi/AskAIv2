using System.Windows;
using System.IO;
using System.Windows.Input;
using System.Collections.Generic;

namespace UI
{
    public partial class SettingsWindow : Window
    {
        private Profile _profile;
        private bool _isCapturingHotkey = false;
        public SettingsWindow(Profile profile)
        {
            InitializeComponent();
            _profile = profile;
            // Set ComboBox selection based on profile
            VoiceProviderComboBox.SelectedIndex = _profile.VoiceProvider == VoiceProvider.Pro ? 1 : 0;
            VoiceProviderComboBox.SelectionChanged += VoiceProviderComboBox_SelectionChanged;
            OverlayHotkeyTextBox.Text = _profile.OverlayHotkey;
            OverlayHotkeyTextBox.TextChanged += OverlayHotkeyTextBox_TextChanged;
            OverlayHotkeyTextBox.GotFocus += OverlayHotkeyTextBox_GotFocus;
            OverlayHotkeyTextBox.PreviewKeyDown += OverlayHotkeyTextBox_PreviewKeyDown;
            UpdateVoskModelButton();
        }

        private void VoiceProviderComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (VoiceProviderComboBox.SelectedIndex == 1)
                _profile.VoiceProvider = VoiceProvider.Pro;
            else
                _profile.VoiceProvider = VoiceProvider.Basic;
            _profile.Save();
        }

        private void EditProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var wizard = new ProfileOnboarding(_profile);
            wizard.Owner = this;
            wizard.ShowDialog();
        }

        private void ResetProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Profile.ProfilePath))
                File.Delete(Profile.ProfilePath);
            MessageBox.Show("Profile reset. Please restart onboarding.", "Profile Reset", MessageBoxButton.OK, MessageBoxImage.Information);
            var wizard = new ProfileOnboarding(new Profile());
            wizard.Owner = this;
            wizard.ShowDialog();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OverlayHotkeyTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _profile.OverlayHotkey = OverlayHotkeyTextBox.Text;
            _profile.Save();
        }

        private void OverlayHotkeyTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _isCapturingHotkey = true;
            OverlayHotkeyTextBox.Text = "Press desired hotkey...";
        }

        private void OverlayHotkeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!_isCapturingHotkey) return;
            e.Handled = true;
            var keys = new List<string>();
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) keys.Add("Ctrl");
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) keys.Add("Alt");
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) keys.Add("Shift");
            if ((Keyboard.Modifiers & ModifierKeys.Windows) == ModifierKeys.Windows) keys.Add("Win");
            if (e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl && e.Key != Key.LeftAlt && e.Key != Key.RightAlt && e.Key != Key.LeftShift && e.Key != Key.RightShift && e.Key != Key.LWin && e.Key != Key.RWin)
                keys.Add(e.Key.ToString());
            var hotkey = string.Join("+", keys);
            OverlayHotkeyTextBox.Text = hotkey;
            _profile.OverlayHotkey = hotkey;
            _profile.Save();
            _isCapturingHotkey = false;
            // Move focus away to exit capture mode
            VoiceProviderComboBox.Focus();
            // Notify MainWindow to re-register hotkey
            if (Application.Current.MainWindow is MainWindow mw)
                mw.ReRegisterHotkey(hotkey);
        }

        private void UpdateVoskModelButton()
        {
            string largeModelPath = Path.Combine("src", "Voice", "vosk-model-plus");
            bool largeModelExists = Directory.Exists(largeModelPath) && File.Exists(Path.Combine(largeModelPath, "README"));
            if (_profile.VoskModel == VoskModelSize.Large)
            {
                VoskModelButton.Content = largeModelExists ? "Enable Small Model" : "Download and Enable Large Model";
            }
            else
            {
                VoskModelButton.Content = largeModelExists ? "Enable Large Model" : "Download and Enable Large Model";
            }
        }

        private async void VoskModelButton_Click(object sender, RoutedEventArgs e)
        {
            string largeModelPath = Path.Combine("src", "Voice", "vosk-model-plus");
            bool largeModelExists = Directory.Exists(largeModelPath) && File.Exists(Path.Combine(largeModelPath, "README"));
            if (!largeModelExists)
            {
                // Download and extract
                string url = "https://alphacephei.com/vosk/models/vosk-model-en-us-0.22.zip";
                string zipPath = Path.Combine(Path.GetTempPath(), "vosk-model-en-us-0.22.zip");
                VoskModelButton.Content = "Downloading...";
                try
                {
                    using (var client = new System.Net.Http.HttpClient())
                    using (var response = await client.GetAsync(url))
                    using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                    // Extract
                    System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, Path.Combine("src", "Voice"), true);
                    // Rename if needed
                    string extracted = Path.Combine("src", "Voice", "vosk-model-en-us-0.22");
                    if (Directory.Exists(extracted) && !Directory.Exists(largeModelPath))
                        Directory.Move(extracted, largeModelPath);
                    File.Delete(zipPath);
                    MessageBox.Show("Large model downloaded and enabled.", "Success");
                    _profile.VoskModel = VoskModelSize.Large;
                    _profile.Save();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to download or extract model: {ex.Message}", "Error");
                }
            }
            else
            {
                // Toggle model
                if (_profile.VoskModel == VoskModelSize.Large)
                    _profile.VoskModel = VoskModelSize.Small;
                else
                    _profile.VoskModel = VoskModelSize.Large;
                _profile.Save();
            }
            UpdateVoskModelButton();
        }
    }
} 