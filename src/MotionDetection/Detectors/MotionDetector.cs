using SkiaSharp;
using System.Threading;

namespace MotionDetection.Detectors;

public abstract class MotionDetector : IMotionDetector
{
    protected int Width;  // image width
    protected int Height; // image height
    protected int pixelsChanged;
    protected byte DetectionNoiseThreshold = 30;
    protected SKColor DetectionColor = SKColor.Parse("#FFAA0000");
    protected byte[] BackgroundFrame;

    public bool MotionLevelCalculation { get; set; }
    public double MotionLevel => (double)pixelsChanged / (Width * Height);

    public double ProcessFrame(SKBitmap image)
    {
        Width = image.Width;
        Height = image.Height;
        var grayMatrix = image.ToGrayScaleArray();

        if (BackgroundFrame == null)
        {
            // alloc memory for a backgound image and for current image
            BackgroundFrame = grayMatrix;

            // just return for the first time
            return 0;
        }

        double diffCount = OnPixelsDiffAction(image, BackgroundFrame, grayMatrix);
        Interlocked.Exchange(ref pixelsChanged, 0);
        var motionLevel = (double)diffCount / (Width * Height);
        BackgroundFrame = grayMatrix;
        return motionLevel;
    }

    protected abstract unsafe int OnPixelsDiffAction(SKBitmap image, byte[] background, byte[] frame);

    public virtual void Reset()
    {
        BackgroundFrame = null;
    }
}
