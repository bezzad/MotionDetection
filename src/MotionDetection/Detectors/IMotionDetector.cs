using SkiaSharp;

namespace MotionDetection.Detectors;

/// <summary>
/// IMotionDetector interface
/// </summary>
public interface IMotionDetector
{
    /// <summary>
    /// Difference threshold value, [1, 255].
    /// </summary>
    /// <remarks><para>The value specifies the amount off difference between pixels, which is treated
    /// as motion pixel.</para>
    /// <para>Default value is set to <b>30</b>.</para>
    /// </remarks>
    int DifferenceThreshold { get; set; }

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
    /// <returns>Motion level - amount of changes in percents</returns>
    double ProcessFrame(SKBitmap image);

    /// <summary>
    /// Reset detector to initial state
    /// </summary>
    void Reset();
}
