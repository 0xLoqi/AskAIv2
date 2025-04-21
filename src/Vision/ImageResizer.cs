using System;
using System.Drawing;

namespace Vision
{
    public static class ImageResizer
    {
        public static void ResizeImage(string inputPath, string outputPath, int maxWidth = 1024)
        {
            using (var image = Image.FromFile(inputPath))
            {
                if (image.Width <= maxWidth)
                {
                    image.Save(outputPath);
                    return;
                }
                int newWidth = maxWidth;
                int newHeight = (int)(image.Height * (maxWidth / (float)image.Width));
                using (var bmp = new Bitmap(image, new Size(newWidth, newHeight)))
                {
                    bmp.Save(outputPath, image.RawFormat);
                }
            }
        }
    }
} 