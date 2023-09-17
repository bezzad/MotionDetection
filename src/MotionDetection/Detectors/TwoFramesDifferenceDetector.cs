using SkiaSharp;
using System;
using System.Threading;

namespace MotionDetection.Detectors;

internal class TwoFramesDifferenceDetector : IMotionDetector
{
    private byte[]? backgroundFrame;
    private int width;  // image width
    private int height; // image height
    private int pixelsChanged;
    private byte threshold = 30;
    private SKColor red = SKColor.Parse("#FFAA0000");

    public bool MotionLevelCalculation { get; set; }
    public double MotionLevel => (double)pixelsChanged / (width * height);

    public double ProcessFrame(SKBitmap image)
    {
        width = image.Width;
        height = image.Height;
        var grayMatrix = image.ToGrayScaleArray();

        if (backgroundFrame == null)
        {
            // alloc memory for a backgound image and for current image
            backgroundFrame = grayMatrix;

            // just return for the first time
            return 0;
        }

        double diffCount = OnPixelsDiffAction(image, backgroundFrame, grayMatrix);
        Interlocked.Exchange(ref pixelsChanged, 0);
        var motionLevel = (double)diffCount / (width * height);
        backgroundFrame = grayMatrix;
        return motionLevel;
    }

    private unsafe int OnPixelsDiffAction(SKBitmap image, byte[] background, byte[] frame)
    {
        byte* dstPtr = (byte*)image.GetPixels().ToPointer();
        SKColorType typeAdj = image.ColorType;
        var colorIndexes = image.GetColorIndexes();
        var updatedPixels = 0;

        for (int i = 0; i < background.Length; i++)
        {
            if (Math.Abs(background[i] - frame[i]) > threshold)
            {
                updatedPixels++;
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

        return updatedPixels;
    }

    public void Reset()
    {
        //backgroundFrame?.Dispose();
        backgroundFrame = null;
    }
}
