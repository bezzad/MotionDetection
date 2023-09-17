using SkiaSharp;
using System.Collections.Generic;

namespace MotionDetection.Detectors;

public class BlobDetector : MotionDetector
{
    private const int CycleBufferCount = 8;
    private List<SKRect> Blobs = new();
    private byte[][] framesCycleBuffer = new byte[CycleBufferCount][];
    private int cycleIndex = 0;

    protected override unsafe int OnPixelsDiffAction(SKBitmap image, byte[] frame)
    {
        var updatedPixels = 0;

        // STEP 1: Read 8 consecutive frames from a video sequence.
        if (cycleIndex < CycleBufferCount)
        {
            framesCycleBuffer[cycleIndex++] = frame;
            return updatedPixels;
        }
        cycleIndex = 0;

        // STEP 2: Subtract all frames with background grayscale image.
        SubtractFrame();

        // STEP 3: Add all the 8 results obtained in STEP 2.
        var sumFrames = SumFrames();

        // STEP 4: In the image obtained in STEP 3, fill all the gaps inside the object.

        // STEP 5: Convert the image obtained in STEP 4 into a binary image.

        // STEP 6: Remove all the extra noises from the image obtained
        //         in STEP 5 using morphological opening operation
        // TODO...

        // STEP 7: find the rectangular bounding box covering
        //         the region of maximum displacement for each object

        byte* dstPtr = (byte*)image.GetPixels().ToPointer();
        var colorIndexes = image.GetColorIndexes();

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                int offset = (y * image.Width + x);
                if (sumFrames[offset] > 0)
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

    private void SubtractFrame()
    {
        foreach (var f in framesCycleBuffer)
        {
            // Frame: = Frame – Background
            SubtractFrame(BackgroundFrame, f);
        }
    }

    private void SubtractFrame(byte[] background, byte[] frame)
    {
        for (int i = 0; i < frame.Length; i++)
        {
            var diff = background[i] - frame[i];
            if (diff > DifferenceThreshold || diff < -DifferenceThreshold)
            {
                frame[i] = 255; // White
            }
            else
            {
                frame[i] = 0; // Black
            }
        }
    }

    private byte[] SumFrames()
    {
        var sumFrame = new byte[Width * Height];
        for (int i = 0; i < sumFrame.Length; i++)
        {
            foreach (var f in framesCycleBuffer)
            {
                sumFrame[i] += f[i];
            }
        }

        return sumFrame;
    }
}
