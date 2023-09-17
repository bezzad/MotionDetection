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

    public double ProcessFrame(SKBitmap motionFrame)
    {
        Interlocked.Exchange(ref Width, motionFrame.Width);
        Interlocked.Exchange(ref Height, motionFrame.Height);
        var grayMotionFrame = motionFrame.ToGrayScaleArray();

        if (BackgroundFrame == null)
        {
            // alloc memory for a backgound image and for current image
            BackgroundFrame = grayMotionFrame;

            // just return for the first time
            return 0;
        }

        if (BackgroundFrame.Length != grayMotionFrame.Length || grayMotionFrame.Length != Width * Height)
        {
            return 0;
        }

        var diffCount = OnPixelsDiffAction(motionFrame, BackgroundFrame, grayMotionFrame);
        Interlocked.Exchange(ref pixelsChanged, diffCount);
        var motionLevel = (double)diffCount / (Width * Height);

        if (diffCount > 0)
        {
            BackgroundFrame = grayMotionFrame;
        }

        return motionLevel;
    }

    protected abstract unsafe int OnPixelsDiffAction(SKBitmap image, byte[] background, byte[] frame);

    public virtual void Reset()
    {
        BackgroundFrame = null;
    }
}
