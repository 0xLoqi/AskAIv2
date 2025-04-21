using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const int HOTKEY_ID = 9000;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_ALT = 0x0001;
    private const uint VK_SPACE = 0x20;
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();

    private Overlay? _overlayWindow;
    private HwndSource? _source;
    private IntPtr _lastActiveWindow = IntPtr.Zero;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;

        // Minimal first-run privacy/consent dialog
        var consentFile = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "user_consent.txt");
        if (!System.IO.File.Exists(consentFile))
        {
            var result = MessageBox.Show(
                "This app may capture your screen and send data to cloud services.\n\nDo you agree to the privacy policy? (See PRIVACY.md)",
                "Privacy & Consent",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );
            if (result == MessageBoxResult.Yes)
            {
                System.IO.File.WriteAllText(consentFile, "agreed");
            }
            else
            {
                Application.Current.Shutdown();
            }
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        this.Hide();
        var helper = new WindowInteropHelper(this);
        _source = HwndSource.FromHwnd(helper.Handle);
        _source.AddHook(HwndHook);
        RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_ALT, VK_SPACE);
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        UnregisterHotKey(helper.Handle, HOTKEY_ID);
        _source?.RemoveHook(HwndHook);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            ToggleOverlay();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void ToggleOverlay()
    {
        if (_overlayWindow == null || !_overlayWindow.IsVisible || !_overlayWindow.IsActive)
        {
            _lastActiveWindow = GetForegroundWindow();
            // Capture screenshot before showing overlay
            string screenshotPath = null;
            if (_lastActiveWindow != IntPtr.Zero)
            {
                screenshotPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"active_window_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                try
                {
                    Vision.ScreenGrabber.CaptureWindowToPng(_lastActiveWindow, screenshotPath);
                }
                catch
                {
                    try
                    {
                        Vision.ScreenGrabber.CaptureScreenToPng(screenshotPath);
                    }
                    catch { screenshotPath = null; }
                }
            }
            _overlayWindow = new Overlay(_lastActiveWindow, screenshotPath);
            _overlayWindow.Owner = this;
            var mousePos = System.Windows.Forms.Control.MousePosition;

            // Find the screen containing the mouse
            var screen = System.Windows.Forms.Screen.FromPoint(mousePos);
            var bounds = screen.WorkingArea;

            // Get DPI scaling for the screen
            double dpiScale = 1.0;
            var source = PresentationSource.FromVisual(this);
            if (source != null)
                dpiScale = source.CompositionTarget.TransformToDevice.M11;

            // Center overlay in the screen
            double overlayWidth = _overlayWindow.Width * dpiScale;
            double overlayHeight = _overlayWindow.Height * dpiScale;
            double left = bounds.Left + (bounds.Width - overlayWidth) / 2.0;
            double top = bounds.Top + (bounds.Height - overlayHeight) / 2.0;

            _overlayWindow.Left = left / dpiScale;
            _overlayWindow.Top = top / dpiScale;
            _overlayWindow.Show();
            _overlayWindow.Closed += (s, args) => { _overlayWindow = null; };
        }
        else
        {
            _overlayWindow.Close();
            _overlayWindow = null;
        }
    }
}