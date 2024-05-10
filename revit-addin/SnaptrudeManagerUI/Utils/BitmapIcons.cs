using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;

namespace SnaptrudeManagerAddin
{
    public class BitmapIcons
    {
        public const int DEFAULT_DPI = 96;
        public int Version { get; set; }

        public string IconFilePath { get; set; }

        private Assembly loadedAssembly { get; set; }

        public BitmapIcons(Assembly loadedAssembly, string imageFile)
        {
            this.IconFilePath = imageFile;
            this.loadedAssembly = loadedAssembly;
        }

        private BitmapSource CreateBitmap(BitmapIcons.IconSize size)
        {
            using (Stream manifestResourceStream = this.loadedAssembly.GetManifestResourceStream(this.IconFilePath))
            {
                Bitmap bitmap = new Bitmap(manifestResourceStream);
                int num1 = (int)size * 2;
                int num2 = 192;
                double num3 = this.ProcessScreenScalefactor();
                manifestResourceStream.Seek(0L, SeekOrigin.Begin);
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = manifestResourceStream;
                bitmapImage.DecodePixelHeight = Convert.ToInt32((double)num1 * num3);
                bitmapImage.EndInit();
                manifestResourceStream.Seek(0L, SeekOrigin.Begin);
                int pixelWidth = bitmapImage.PixelWidth;
                PixelFormat format = bitmapImage.Format;
                int num4 = bitmapImage.Format.BitsPerPixel / 8;
                BitmapPalette palette = bitmapImage.Palette;
                int stride = pixelWidth * num4;
                Array instance = Array.CreateInstance(typeof(byte), stride * pixelWidth);
                bitmapImage.CopyPixels(instance, stride, 0);
                return BitmapSource.Create(Convert.ToInt32((double)num1 * num3), Convert.ToInt32((double)num1 * num3), (double)num2 * num3, (double)num2 * num3, format, palette, instance, stride);
            }
        }

        private double ProcessScreenScalefactor()
        {
            return 1.0;
        }

        public BitmapSource SmallBitmap() => this.CreateBitmap(BitmapIcons.IconSize.ICON_SMALL);

        public BitmapSource MediumBitmap() => this.CreateBitmap(BitmapIcons.IconSize.ICON_MEDIUM);

        public BitmapSource LargeBitmap() => this.CreateBitmap(BitmapIcons.IconSize.ICON_LARGE);

        public enum IconSize
        {
            ICON_SMALL = 16, // 0x00000010
            ICON_MEDIUM = 24, // 0x00000018
            ICON_LARGE = 32, // 0x00000020
        }
    }
}
