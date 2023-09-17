using SkiaSharp;
using System.Threading;

namespace MotionDetection.Detectors;

public abstract class MotionDetector : IMotionDetector
{
    protected int Width;  // image width
    protected int Height; // image height
    protected int pixelsChanged;
    protected SKColor DetectionColor = SKColor.Parse("#FFAA0000");
    protected byte[] BackgroundFrame;

    public int DifferenceThreshold { get; set; } = 30;
    public bool MotionLevelCalculation { get; set; }
    public double MotionLevel => (double)pixelsChanged / (Width * Height);

    public double ProcessFrame(SKBitmap frame)
    {
        var grayscaleFrame = frame.Mirror().ToGrayScaleArray();
        Interlocked.Exchange(ref Width, frame.Width);
        Interlocked.Exchange(ref Height, frame.Height);

        if (BackgroundFrame == null)
        {
            // alloc memory for a backgound image and for current image
            UpdateBackground(grayscaleFrame);

            // just return for the first time
            return 0;
        }

        if (BackgroundFrame.Length != grayscaleFrame.Length ||
            grayscaleFrame.Length != Width * Height)
        {
            return 0;
        }

        Interlocked.Exchange(ref pixelsChanged, OnPixelsDiffAction(frame, grayscaleFrame));
        return MotionLevel;
    }

    protected abstract unsafe int OnPixelsDiffAction(SKBitmap image, byte[] frame);

    protected void UpdateBackground(byte[] background)
    {
        Interlocked.Exchange(ref BackgroundFrame, background);
    }

    public virtual void Reset()
    {
        BackgroundFrame = null;
    }
}
