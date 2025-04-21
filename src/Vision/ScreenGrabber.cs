using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Vision
{
    public static class ScreenGrabber
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public static void CaptureActiveWindowToPng(string outputPath)
        {
            IntPtr hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero)
                throw new InvalidOperationException("No active window found.");

            if (!GetWindowRect(hWnd, out RECT rect))
                throw new InvalidOperationException("Failed to get window rect.");

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            if (width <= 0 || height <= 0)
                throw new InvalidOperationException("Invalid window size.");

            using var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height));
            }
            bmp.Save(outputPath, ImageFormat.Png);
        }

        public static void CaptureWindowToPng(IntPtr hWnd, string outputPath)
        {
            if (hWnd == IntPtr.Zero)
                throw new InvalidOperationException("No window handle provided.");

            if (!GetWindowRect(hWnd, out RECT rect))
                throw new InvalidOperationException("Failed to get window rect.");

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            if (width <= 0 || height <= 0)
                throw new InvalidOperationException("Invalid window size.");

            using var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height));
            }
            bmp.Save(outputPath, ImageFormat.Png);
        }

        public static void CaptureScreenToPng(string outputPath)
        {
            var bounds = Screen.PrimaryScreen.Bounds;
            using var bmp = new Bitmap(bounds.Width, bounds.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bmp.Size);
            }
            bmp.Save(outputPath, ImageFormat.Png);
        }
    }
} 