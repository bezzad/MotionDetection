using SkiaSharp;

namespace MotionDetection.Detectors;

public class TwoFramesDifferenceDetector : MotionDetector
{
    protected override unsafe int OnPixelsDiffAction(SKBitmap image, byte[] background, byte[] frame)
    {
        byte* dstPtr = (byte*)image.GetPixels().ToPointer();
        var colorIndexes = image.GetColorIndexes();
        var updatedPixels = 0;

        for (int i = 0; i < background.Length; i++)
        {
            var diff = background[i] - frame[i];
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

        return updatedPixels;
    }
}
