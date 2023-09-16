using SkiaSharp;

namespace MotionDetection.Detectors;

/// <summary>
/// IMotionDetector interface
/// </summary>
public interface IMotionDetector
{
    /// <summary>
    /// Motion level calculation - calculate or not motion level
    /// </summary>
    bool MotionLevelCalculation { set; get; }

    /// <summary>
    /// Motion level - amount of changes in percents
    /// </summary>
    double MotionLevel { get; }

    /// <summary>
    /// Process new frame
    /// </summary>
    void ProcessFrame(SKBitmap image);

    /// <summary>
    /// Reset detector to initial state
    /// </summary>
    void Reset();
}
