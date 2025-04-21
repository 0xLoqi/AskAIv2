using System;
using Vision;

class Program
{
    static void Main(string[] args)
    {
        string outputPath = "output/active_window.png";
        try
        {
            ScreenGrabber.CaptureActiveWindowToPng(outputPath);
            Console.WriteLine($"Active window captured to: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Capture failed: {ex.Message}");
        }
    }
}
