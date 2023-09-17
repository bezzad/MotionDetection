using SkiaSharp;
using System;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

namespace MotionDetection
{
    public static class Extension
    {
        private static SKColorFilter GrayScaleFilter = SKColorFilter.CreateColorMatrix(new float[]
                {
                    0.2126f, 0.7152f, 0.0722f, 0, 0,  // red channel weights
                    0.2126f, 0.7152f, 0.0722f, 0, 0,  // green channel weights
                    0.2126f, 0.7152f, 0.0722f, 0, 0,  // blue channel weights
                    0,       0,       0,       1, 0   // alpha channel weights
                });

        public static unsafe SKBitmap Mirror(this SKBitmap srcBitmap)
        {
            using var canvas = new SKCanvas(srcBitmap);
            canvas.Scale(-1, 1, srcBitmap.Width / 2.0f, 0);
            canvas.DrawBitmap(srcBitmap, new SKPoint(0.0f, 0.0f));
            return srcBitmap;
        }

        public static unsafe SKBitmap CloneToRgba8888(this SKBitmap srcBitmap)
        {
            var width = srcBitmap.Width;
            var height = srcBitmap.Height;
            var typeOrg = srcBitmap.ColorType;
            var typeAdj = SKColorType.Rgba8888;
            var dstBitmap = new SKBitmap(width, height, typeAdj, srcBitmap.AlphaType, srcBitmap.ColorSpace);

            byte* srcPtr = (byte*)srcBitmap.GetPixels().ToPointer();
            byte* dstPtr = (byte*)dstBitmap.GetPixels().ToPointer();

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    // Get color from original bitmap
                    byte byte1 = *srcPtr++;  // red or blue
                    byte byte2 = *srcPtr++;  // green
                    byte byte3 = *srcPtr++;  // blue or red
                    byte byte4 = *srcPtr++;  // alpha

                    // Store the bytes in the adjusted bitmap
                    if (typeOrg == SKColorType.Rgba8888)
                    {
                        *dstPtr++ = byte1;
                        *dstPtr++ = byte2;
                        *dstPtr++ = byte3;
                        *dstPtr++ = byte4;
                    }
                    else if (typeOrg == SKColorType.Bgra8888)
                    {
                        *dstPtr++ = byte3;
                        *dstPtr++ = byte2;
                        *dstPtr++ = byte1;
                        *dstPtr++ = byte4;
                    }
                }
            }

            return dstBitmap;
        }

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
            paint.ColorFilter = GrayScaleFilter;
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

        public static byte[] ApplyMedianFilter(this byte[] grayImageBytes, int width, int height)
        {
            if (grayImageBytes.Length != width * height)
                throw new IndexOutOfRangeException(nameof(grayImageBytes));

            var maskMatrix = new byte[9];
            // center (4i) = y*w+x
            // 0i:  center + y - 1              ______________
            // 1i:  center - y                 | 0i | 1i | 2i |
            // 2i:  center - y + 1             |____|____|____|
            // 3i:  center - 1                 | 3i | 4i | 5i |
            // 4i:  center                     |____|____|____|
            // 5i:  center + 1                 | 6i | 7i | 8i |
            // 6i:  center + y - 1             |____|____|____|
            // 7i:  center + y 
            // 8i:  center + y + 1
            // 

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    var center = y * width + x;
                    maskMatrix[0] = grayImageBytes[center + y - 1];
                    maskMatrix[1] = grayImageBytes[center - y];
                    maskMatrix[2] = grayImageBytes[center - y + 1];
                    maskMatrix[3] = grayImageBytes[center - 1];
                    maskMatrix[4] = grayImageBytes[center];
                    maskMatrix[5] = grayImageBytes[center + 1];
                    maskMatrix[6] = grayImageBytes[center + y - 1];
                    maskMatrix[7] = grayImageBytes[center + y];
                    maskMatrix[8] = grayImageBytes[center + y + 1];

                    Array.Sort(maskMatrix);
                    grayImageBytes[center] = maskMatrix[4];
                }
            }

            return grayImageBytes;
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
                    byte red = pixelArray[y, x, 0];
                    byte green = pixelArray[y, x, 1];
                    byte blue = pixelArray[y, x, 2];
                    byte alpha = pixelArray[y, x, 3];
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

        public static SKBitmap ToImage(this byte[] pixelValues, int width, int height)
        {
            SKBitmap bitmap = new();
            GCHandle gcHandle = GCHandle.Alloc(pixelValues, GCHandleType.Pinned);
            SKImageInfo info = new(width, height, SKColorType.Gray8, SKAlphaType.Unpremul);

            IntPtr ptr = gcHandle.AddrOfPinnedObject();
            int rowBytes = info.RowBytes;
            bitmap.InstallPixels(info, ptr, rowBytes, delegate { gcHandle.Free(); });

            return bitmap;
        }

        public static byte[,,] ToArray(this SKBitmap bmp)
        {
            ReadOnlySpan<byte> spn = bmp.GetPixelSpan();

            byte[,,] pixelValues = new byte[bmp.Height, bmp.Width, 4];
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    int offset = (y * bmp.Width + x) * bmp.BytesPerPixel;
                    if (bmp.ColorType == SKColorType.Bgra8888)
                    {
                        pixelValues[y, x, 0] = spn[offset + 2]; // Red
                        pixelValues[y, x, 1] = spn[offset + 1]; // Green
                        pixelValues[y, x, 2] = spn[offset + 0]; // Blue
                        pixelValues[y, x, 3] = spn[offset + 3]; // Alpha
                    }
                    else
                    {
                        pixelValues[y, x, 0] = spn[offset + 0]; // Red
                        pixelValues[y, x, 1] = spn[offset + 1]; // Green
                        pixelValues[y, x, 2] = spn[offset + 2]; // Blue
                        pixelValues[y, x, 3] = spn[offset + 3]; // Alpha
                    }
                }
            }

            return pixelValues;
        }
    }
}
