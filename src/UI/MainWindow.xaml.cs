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

    private Overlay? _overlayWindow;
    private HwndSource? _source;

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
        if (_overlayWindow == null)
        {
            _overlayWindow = new Overlay();
            var mousePos = System.Windows.Forms.Control.MousePosition;
            _overlayWindow.Left = mousePos.X;
            _overlayWindow.Top = mousePos.Y;
            _overlayWindow.Show();
            _overlayWindow.Closed += (s, args) => { _overlayWindow = null; };
        }
        else
        {
            _overlayWindow.SafeClose();
            _overlayWindow = null;
        }
    }
}