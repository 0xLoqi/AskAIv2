using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace UI
{
    /// <summary>
    /// Base class that gives any overlay "pin + auto-hide" behaviour
    /// without relying on fragile event order.
    /// </summary>
    public class OverlayWindow : Window
    {
        #region  — Pin state —

        public static readonly DependencyProperty IsPinnedProperty =
            DependencyProperty.Register(nameof(IsPinned), typeof(bool),
                typeof(OverlayWindow), new PropertyMetadata(false));

        public bool IsPinned
        {
            get => (bool)GetValue(IsPinnedProperty);
            set => SetValue(IsPinnedProperty, value);
        }

        #endregion

        #region  — Win32 + WPF initialisation —

        private const int WM_ACTIVATEAPP = 0x001C;
        private bool _isScheduledToClose;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Raw hook so we catch Alt‑Tab, Win‑D, task‑bar clicks, etc.
            var src = (HwndSource)PresentationSource.FromVisual(this)!;
            src.AddHook(WndProc);

            // Guarantee focus, otherwise we'll never receive Deactivated.
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                                   new Action(() => { Activate(); Keyboard.Focus(this); }));
        }

        #endregion

        #region  — Auto‑hide logic (all safe‑queued) —

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            MaybeScheduleClose();
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);
            // Only hide when focus leaves the *overlay*, not when it moves to a child
            if (e.NewFocus == null || !IsAncestorOf(e.NewFocus as DependencyObject))
                MaybeScheduleClose();
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            // If the click landed *outside* this window's visuals, hide (unless pinned).
            if (!IsPinned && IsMouseOver == false)
                MaybeScheduleClose();
        }

        private void MaybeScheduleClose()
        {
            if (IsPinned || _isScheduledToClose) return;

            _isScheduledToClose = true;
            // Run after current input message completes → no re‑entrancy crash.
            Dispatcher.BeginInvoke(() =>
            {
                if (!IsPinned) Close();
            }, DispatcherPriority.Normal);
        }

        #endregion

        #region  — Win32 hook —

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_ACTIVATEAPP && wParam == IntPtr.Zero)   // app lost foreground
                MaybeScheduleClose();

            return IntPtr.Zero;
        }

        #endregion
    }
} 