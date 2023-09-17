using SkiaSharp;
using System;
using System.Runtime.InteropServices;

namespace MotionDetection
{
    public static class Extension
    {
        private static SKColorFilter grayScaleFilter = SKColorFilter.CreateColorMatrix(new float[]
                {
                    0.2126f, 0.7152f, 0.0722f, 0, 0,  // red channel weights
                    0.2126f, 0.7152f, 0.0722f, 0, 0,  // green channel weights
                    0.2126f, 0.7152f, 0.0722f, 0, 0,  // blue channel weights
                    0,       0,       0,       1, 0   // alpha channel weights
                });

        public static unsafe void ToPosterizeImage(this SKBitmap bitmap)
        {
            uint* ptr = (uint*)bitmap.GetPixels().ToPointer();
            int pixelCount = bitmap.Width * bitmap.Height;

            for (int i = 0; i < pixelCount; i++)
            {
                *ptr++ &= 0xE0E0E0FF;
            }
        }

        public static void ToGrayScaleImage(this SKBitmap bitmap)
        {
            using var canvas = new SKCanvas(bitmap);
            using SKPaint paint = new SKPaint();
            // Define a grayscale color filter to apply to the image
            paint.ColorFilter = grayScaleFilter;
            canvas.DrawBitmap(bitmap, bitmap.Info.Rect, paint);
        }

        public static byte[] ToGrayScaleArray(this SKBitmap bitmap)
        {
            ReadOnlySpan<byte> spn = bitmap.GetPixelSpan();
            var width = bitmap.Width;
            var height = bitmap.Height;
            var grayScaleArray = new byte[width * height];
            var colorIndexes = bitmap.GetColorIndexes();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // grayscale value using BT709
                    int offset = (y * width + x) * bitmap.BytesPerPixel;
                    grayScaleArray[y * width + x] = (byte)(0.2125f * spn[offset + colorIndexes.Red] + 0.7154f * spn[offset + colorIndexes.Green] + 0.0721f * spn[colorIndexes.Blue]);
                }
            }

            return grayScaleArray;
        }

        public static (int Red, int Green, int Blue, int Alpha) GetColorIndexes(this SKBitmap bitmap)
        {
            return bitmap.ColorType switch
            {
                SKColorType.Bgra8888 => (2, 1, 0, 3),
                SKColorType.Rgba8888 => (0, 1, 2, 3),
                SKColorType.Argb4444 => (1, 2, 3, 0),
                SKColorType.Rgba16161616 => (0, 1, 2, 3),
                SKColorType.Rgb888x => (0, 1, 2, 0),
                SKColorType.Gray8 => (0, 0, 0, 0),
                _ => (0, 1, 2, 3),
            };
        }

        public static SKBitmap ToImage(this byte[,,] pixelArray)
        {
            int width = pixelArray.GetLength(1);
            int height = pixelArray.GetLength(0);

            uint[] pixelValues = new uint[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte alpha = 255;
                    byte red = pixelArray[y, x, 0];
                    byte green = pixelArray[y, x, 1];
                    byte blue = pixelArray[y, x, 2];
                    uint pixelValue = (uint)red + (uint)(green << 8) + (uint)(blue << 16) + (uint)(alpha << 24);
                    pixelValues[y * width + x] = pixelValue;
                }
            }

            SKBitmap bitmap = new();
            GCHandle gcHandle = GCHandle.Alloc(pixelValues, GCHandleType.Pinned);
            SKImageInfo info = new(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);

            IntPtr ptr = gcHandle.AddrOfPinnedObject();
            int rowBytes = info.RowBytes;
            bitmap.InstallPixels(info, ptr, rowBytes, delegate { gcHandle.Free(); });

            return bitmap;
        }

        public static byte[,,] ToArray(this SKBitmap bmp)
        {
            ReadOnlySpan<byte> spn = bmp.GetPixelSpan();

            byte[,,] pixelValues = new byte[bmp.Height, bmp.Width, 3];
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    int offset = (y * bmp.Width + x) * bmp.BytesPerPixel;
                    pixelValues[y, x, 0] = spn[offset + 2];
                    pixelValues[y, x, 1] = spn[offset + 1];
                    pixelValues[y, x, 2] = spn[offset + 0];
                }
            }

            return pixelValues;
        }
    }
}
