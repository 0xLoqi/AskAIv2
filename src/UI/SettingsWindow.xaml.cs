using System.Windows;
using System.IO;

namespace UI
{
    public partial class SettingsWindow : Window
    {
        private Profile _profile;
        public SettingsWindow(Profile profile)
        {
            InitializeComponent();
            _profile = profile;
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
    }
} 