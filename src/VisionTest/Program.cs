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
            // Test ImageResizer
            string resizedPath = "output/active_window_resized.png";
            ImageResizer.ResizeImage(outputPath, resizedPath);
            Console.WriteLine($"Resized image saved to: {resizedPath}");
            // Test VisionClient
            string prompt = "What is shown in this screenshot?";
            string answer = VisionClient.AskWithImageAsync(resizedPath, prompt).Result;
            Console.WriteLine($"Vision answer: {answer}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Capture failed: {ex.Message}");
        }
    }
}
