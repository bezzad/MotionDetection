using Epoxy;
using FlashCap;
using SkiaSharp;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MotionDetection.ViewModels;

[ViewModel]
public sealed class MainWindowViewModel
{
    private long countFrames;
    private long lastCountFrames;
    private double lastFrameTime;
    private double realFps;

    // Constructed capture device.
    private CaptureDevice? captureDevice;

    // Binding members.
    public Command? Loaded { get; }
    public SKBitmap? Image { get; private set; }
    public string? Device { get; private set; }
    public string? Characteristics { get; private set; }

    public string? Statistics1 { get; private set; }
    public string? Statistics2 { get; private set; }
    public string? Statistics3 { get; private set; }

    public MainWindowViewModel()
    {
        // Window shown:
        Loaded = Command.Factory.Create(async () =>
        {
            ////////////////////////////////////////////////
            // Initialize and start capture device

            // Enumerate capture devices:
            var devices = new CaptureDevices();
            var descriptors = devices.EnumerateDescriptors().
                // You could filter by device type and characteristics.
                //Where(d => d.DeviceType == DeviceTypes.DirectShow).  // Only DirectShow device.
                Where(d => d.Characteristics.Length >= 1).             // One or more valid video characteristics.
                ToArray();

            // Use first device.
            if (descriptors.ElementAtOrDefault(0) is { } descriptor0)
            {
                Device = descriptor0.ToString();

                // Or, you could choice from device descriptor:
                // Hint: Show up video characteristics into ComboBox and like.
                var characteristics = descriptor0.Characteristics.
                    FirstOrDefault(c => c.PixelFormat != PixelFormats.Unknown);

                if (characteristics != null)
                {
                    // Show status.
                    Characteristics = characteristics.ToString();

                    // Open capture device:
                    captureDevice = await descriptor0.OpenAsync(
                        characteristics,
                        OnPixelBufferArrivedAsync);

                    // Start capturing.
                    await captureDevice.StartAsync();
                }
                else
                {
                    Characteristics = "(Formats are not found)";
                }
            }
            else
            {
                Device = "(Devices are not found)";
            }
        });
    }

    private async Task OnPixelBufferArrivedAsync(PixelBufferScope bufferScope)
    {
        ////////////////////////////////////////////////
        // Pixel buffer has arrived.
        // NOTE: Perhaps this thread context is NOT UI thread.
        // Or, refer image data binary directly.
        ArraySegment<byte> image = bufferScope.Buffer.ReferImage();

        // Decode image data to a bitmap:
        var bitmap = SKBitmap.Decode(image);

        // Capture statistics variables.
        var countFrames = Interlocked.Increment(ref this.countFrames);
        var frameIndex = bufferScope.Buffer.FrameIndex;
        var timestamp = bufferScope.Buffer.Timestamp;
        var avgFps = countFrames / timestamp.TotalSeconds;
        var duration = timestamp.TotalMilliseconds - lastFrameTime;

        if (duration > 100)
        {
            Interlocked.Exchange(ref realFps, (countFrames - lastCountFrames) / (duration / 1000));
            Interlocked.Exchange(ref lastCountFrames, countFrames);
            Interlocked.Exchange(ref lastFrameTime, timestamp.TotalMilliseconds);
        }

        // `bitmap` is copied, so we can release pixel buffer now.
        bufferScope.ReleaseNow();

        // Switch to UI thread:
        if (await UIThread.TryBind())
        {
            // Update a bitmap.
            Image = bitmap;

            // Update statistics.
            this.Statistics1 = $"Frame={countFrames}/{frameIndex}";
            this.Statistics2 = $"FPS={realFps:F3} |  Avg{avgFps:F3}";
            this.Statistics3 = $"SKBitmap={bitmap.Width}x{bitmap.Height} [{bitmap.ColorType}]";
        }
    }
}

