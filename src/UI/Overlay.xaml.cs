using System.Windows;
using System.Windows.Threading;

namespace UI
{
    public partial class Overlay : Window
    {
        private bool _isPinned = false;
        private bool _isClosing = false;
        private bool _pinButtonClicked = false;
        public void SafeClose()
        {
            if (_isClosing) return;
            _isClosing = true;
            this.Close();
        }
        public Overlay()
        {
            InitializeComponent();
            this.Activate();
            PinButton.PreviewMouseDown += (s, e) => _pinButtonClicked = true;
            PinButton.Checked += (s, e) => _pinButtonClicked = false; _isPinned = true;
            PinButton.Unchecked += (s, e) => _pinButtonClicked = false; _isPinned = false;
            this.Deactivated += (s, e) => {
                if (_pinButtonClicked) { _pinButtonClicked = false; return; }
                if (!_isPinned) SafeClose();
            };
            this.LostKeyboardFocus += (s, e) => { if (!_isPinned) SafeClose(); };
            this.LostMouseCapture += (s, e) => { if (!_isPinned) SafeClose(); };
            this.PreviewMouseDown += Overlay_PreviewMouseDown;
        }

        private void Overlay_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isPinned) return;
            // If the click is not on the window or its children, close
            if (e.OriginalSource is not System.Windows.DependencyObject depObj || !this.IsAncestorOf(depObj))
            {
                SafeClose();
            }
        }

        public bool IsPinned() => _isPinned;
    }
} 