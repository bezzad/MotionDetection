using SkiaSharp;
using System;
using System.Threading;

namespace MotionDetection.Detectors;

internal class MotionDetector1 : IMotionDetector
{
    private byte[]? backgroundFrame;
    private int width;  // image width
    private int height; // image height
    private int pixelsChanged;
    private byte threshold = 30;
    private SKColor red = SKColor.Parse("#FFAA0000");

    public bool MotionLevelCalculation { get; set; }
    public double MotionLevel => (double)pixelsChanged / (width * height);

    public void ProcessFrame(SKBitmap image)
    {
        width = image.Width;
        height = image.Height;
        var grayMatrix = image.ToGrayScaleArray();

        if (backgroundFrame == null)
        {
            // alloc memory for a backgound image and for current image
            backgroundFrame = grayMatrix;

            // just return for the first time
            return;
        }

        OnPixelAction(image, backgroundFrame, grayMatrix);
        backgroundFrame = grayMatrix;
    }

    private unsafe void OnPixelAction(SKBitmap image, byte[] background, byte[] frame)
    {
        byte* dstPtr = (byte*)image.GetPixels().ToPointer();
        SKColorType typeAdj = image.ColorType;
        var colorIndexes = image.GetColorIndexes();

        for (int i = 0; i < background.Length; i++)
        {
            if (Math.Abs(background[i] - frame[i]) > threshold)
            {
                Interlocked.Increment(ref pixelsChanged);
                // Store the bytes in the adjusted bitmap
                if (colorIndexes.Red < colorIndexes.Green)
                {
                    *dstPtr++ = red.Red;
                    *dstPtr++ = red.Green;
                    *dstPtr++ = red.Blue;
                    *dstPtr++ = red.Alpha;
                }
                else
                {
                    *dstPtr++ = red.Blue;
                    *dstPtr++ = red.Green;
                    *dstPtr++ = red.Red;
                    *dstPtr++ = red.Alpha;
                }
            }
            else
            {
                dstPtr += image.BytesPerPixel;
            }
        }
    }

    public void Reset()
    {
        //backgroundFrame?.Dispose();
        backgroundFrame = null;
    }
}
