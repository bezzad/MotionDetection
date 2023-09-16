using SkiaSharp;

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


        public static void ToGrayScale(this SKBitmap bitmap)
        {
            using var canvas = new SKCanvas(bitmap);
            using SKPaint paint = new SKPaint();
            // Define a grayscale color filter to apply to the image
            paint.ColorFilter = grayScaleFilter;
            canvas.DrawBitmap(bitmap, bitmap.Info.Rect, paint);
        }

        public static byte[] ToGrayScaleMatrix(this SKBitmap bitmap)
        {
            var width = bitmap.Width;
            var height = bitmap.Height;
            var grayScaleArray = new byte[width * height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    // grayscale value using BT709
                    grayScaleArray[width * y + x] = (byte)(0.2125f * pixel.Red + 0.7154f * pixel.Green + 0.0721f * pixel.Blue);
                }
            }

            return grayScaleArray;
        }
    }
}
