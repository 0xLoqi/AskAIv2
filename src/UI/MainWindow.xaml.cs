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

    public Overlay? _overlayWindow;
    private HwndSource? _source;
    private IntPtr _lastActiveWindow = IntPtr.Zero;
    private string _currentHotkey = "Alt+Space";
    private uint _currentModifiers = MOD_ALT;
    private uint _currentKey = VK_SPACE;
    private Profile _profile = Profile.Load();

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

        // Preload overlay window (hidden)
        _lastActiveWindow = GetForegroundWindow();
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
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        this.Hide();
        if (_overlayWindow != null)
        {
            _overlayWindow.Owner = this;
            _overlayWindow.Show();
            _overlayWindow.Activate();
        }
        var helper = new WindowInteropHelper(this);
        _source = HwndSource.FromHwnd(helper.Handle);
        _source.AddHook(HwndHook);
        // Register hotkey from profile
        (_currentModifiers, _currentKey) = ParseHotkey(_profile.OverlayHotkey);
        RegisterHotKey(helper.Handle, HOTKEY_ID, _currentModifiers, _currentKey);
        _currentHotkey = _profile.OverlayHotkey;
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        UnregisterHotKey(helper.Handle, HOTKEY_ID);
        _source?.RemoveHook(HwndHook);
        if (_overlayWindow != null)
        {
            _overlayWindow.Close();
            _overlayWindow = null;
        }
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
        if (_overlayWindow == null)
            return;
        if (!_overlayWindow.IsVisible)
        {
            _lastActiveWindow = GetForegroundWindow();
            // Update screenshot before showing overlay
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
            _overlayWindow._previousActiveWindow = _lastActiveWindow;
            _overlayWindow._lastScreenshotPath = screenshotPath;
            _overlayWindow.Show();
            _overlayWindow.Activate();
        }
        else
        {
            _overlayWindow.Hide();
        }
    }

    public void ReRegisterHotkey(string hotkey)
    {
        var helper = new WindowInteropHelper(this);
        UnregisterHotKey(helper.Handle, HOTKEY_ID);
        (_currentModifiers, _currentKey) = ParseHotkey(hotkey);
        RegisterHotKey(helper.Handle, HOTKEY_ID, _currentModifiers, _currentKey);
        _currentHotkey = hotkey;
    }

    private (uint, uint) ParseHotkey(string hotkey)
    {
        uint mods = 0;
        uint key = 0;
        if (string.IsNullOrWhiteSpace(hotkey)) return (MOD_ALT, VK_SPACE);
        var parts = hotkey.Split('+');
        foreach (var part in parts)
        {
            var p = part.Trim().ToLower();
            if (p == "ctrl") mods |= MOD_CONTROL;
            else if (p == "alt") mods |= MOD_ALT;
            else if (p == "shift") mods |= MOD_SHIFT;
            else if (p == "win") mods |= 0x0008; // MOD_WIN
            else
            {
                try
                {
                    var k = (System.Windows.Forms.Keys)Enum.Parse(typeof(System.Windows.Forms.Keys), part, true);
                    key = (uint)k;
                }
                catch { }
            }
        }
        if (key == 0) key = VK_SPACE;
        return (mods, key);
    }
}