using SkiaSharp;

namespace MotionDetection.Detectors;

public class TwoFramesDifferenceDetector : MotionDetector
{
    protected override unsafe int OnPixelsDiffAction(SKBitmap image, byte[] frame)
    {
        byte* dstPtr = (byte*)image.GetPixels().ToPointer();
        var colorIndexes = image.GetColorIndexes();
        var updatedPixels = 0;

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                int offset = (y * image.Width + x);
                var diff = BackgroundFrame[offset] - frame[offset];
                if (diff > DifferenceThreshold || diff < -DifferenceThreshold)
                {
                    updatedPixels++;
                    // Store the bytes in the adjusted bitmap
                    if (colorIndexes.Red < colorIndexes.Green)
                    {
                        *dstPtr++ = DetectionColor.Red;
                        *dstPtr++ = DetectionColor.Green;
                        *dstPtr++ = DetectionColor.Blue;
                        *dstPtr++ = DetectionColor.Alpha;
                    }
                    else
                    {
                        *dstPtr++ = DetectionColor.Blue;
                        *dstPtr++ = DetectionColor.Green;
                        *dstPtr++ = DetectionColor.Red;
                        *dstPtr++ = DetectionColor.Alpha;
                    }
                }
                else
                {
                    dstPtr += image.BytesPerPixel;
                }
            }
        }

        UpdateBackground(frame);
        return updatedPixels;
    }
}
