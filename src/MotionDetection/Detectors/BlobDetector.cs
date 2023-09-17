using SkiaSharp;
using System.Collections.Generic;

namespace MotionDetection.Detectors;

public class BlobDetector : MotionDetector
{
    private List<byte[]> framesBuffer = new();
    private List<SKRect> Blobs = new();

    protected override unsafe int OnPixelsDiffAction(SKBitmap image, byte[] background, byte[] frame)
    {
        if (framesBuffer.Count < 8)
        {
            framesBuffer.Add(frame); 
            return 0;
        }

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
