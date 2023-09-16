using SkiaSharp;
using System;

namespace MotionDetection.Detectors;

internal class MotionDetector1 : IMotionDetector
{
    private byte[] backgroundFrame;
    private int width;  // image width
    private int height; // image height
    private int pixelsChanged;
    private byte threshold = 20;
    private SKColor red = SKColor.Parse("#FFAA0000");

    public bool MotionLevelCalculation { get; set; }
    public double MotionLevel => (double)pixelsChanged / (width * height);

    public void ProcessFrame(SKBitmap image)
    {
        width = image.Width;
        height = image.Height;
        var grayMatrix = image.ToGrayScaleMatrix();

        if (backgroundFrame == null)
        {
            // alloc memory for a backgound image and for current image
            backgroundFrame = grayMatrix;

            // just return for the first time
            return;
        }

        OnPixelAction(image, backgroundFrame, grayMatrix);
    }

    private unsafe void OnPixelAction(SKBitmap image, byte[] background, byte[] frame)
    {
        byte* dstPtr = (byte*)image.GetPixels().ToPointer();
        SKColorType typeAdj = image.ColorType;

        for (int i = 0; i < background.Length; i++)
        {
            if (Math.Abs(background[i] - frame[i]) > threshold)
            {
                //image.SetPixel(i % height, i / height, red);

                // Store the bytes in the adjusted bitmap
                if (typeAdj == SKColorType.Rgba8888)
                {
                    *dstPtr++ = red.Red;
                    *dstPtr++ = red.Green;
                    *dstPtr++ = red.Blue;
                    *dstPtr++ = red.Alpha;
                }
                else if (typeAdj == SKColorType.Bgra8888)
                {
                    *dstPtr++ = red.Blue;
                    *dstPtr++ = red.Green;
                    *dstPtr++ = red.Red;
                    *dstPtr++ = red.Alpha;
                }
            }
            else
            {
                dstPtr += 4;
            }
        }
    }

    public void Reset()
    {
        //backgroundFrame?.Dispose();
        backgroundFrame = null;
    }
}
