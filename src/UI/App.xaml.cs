using System.Configuration;
using System.Data;
using System.Windows;
using System.Threading.Tasks;

namespace UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Task.Run(() => {
            string modelPath = Environment.GetEnvironmentVariable("VOSK_MODEL_PATH") ?? @"C:\Users\Elijah\Documents\Coding Projects\AskAI\src\Voice\vosk-model";
            while (true) // Always-on loop
            {
                Voice.VoskHotwordListener.ListenForHotword(modelPath, "sky", () =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var mainWindow = Application.Current.MainWindow as MainWindow;
                        if (mainWindow != null && mainWindow._overlayWindow != null)
                        {
                            var overlay = mainWindow._overlayWindow;
                            overlay.Show();
                            overlay.Activate();
                            overlay.StartVoiceDictationByHotword();
                        }
                    });
                });
                // Small delay before restarting listener
                System.Threading.Thread.Sleep(500);
            }
        });
    }
}

