using Epoxy;
using Epoxy.Synchronized;
using FlashCap;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    public Command? Opened { get; }
    public Command? Loaded { get; }
    public bool IsEnbaled { get; private set; }
    public SKBitmap? Image { get; private set; }

    public ObservableCollection<CaptureDeviceDescriptor?> DeviceList { get; } = new();
    public CaptureDeviceDescriptor? Device { get; set; }

    public ObservableCollection<VideoCharacteristics> CharacteristicsList { get; } = new();
    public VideoCharacteristics? Characteristics { get; set; }

    public string? Statistics1 { get; private set; }
    public string? Statistics2 { get; private set; }
    public string? Statistics3 { get; private set; }

    public MainWindowViewModel()
    {
        // Window shown:
        Loaded = Command.Factory.CreateSync(() =>
        {
            ////////////////////////////////////////////////
            // Initialize and start capture device

            // Enumerate capture devices:
            var devices = new CaptureDevices();

            // Store device list into the combo box.
            DeviceList.Clear();

            foreach (var descriptor in devices.EnumerateDescriptors().
                // You could filter by device type and characteristics.
                //Where(d => d.DeviceType == DeviceTypes.DirectShow).  // Only DirectShow device.
                Where(d => d.Characteristics.Length >= 1))             // One or more valid video characteristics.
            {
                DeviceList.Add(descriptor);
            }

            IsEnbaled = true;

            // select one camera from trusted place
            Device = DeviceList.FirstOrDefault();
        });
    }

    // Devices combo box was changed.
    [PropertyChanged(nameof(Device))]
    private ValueTask OnDeviceListChangedAsync(CaptureDeviceDescriptor? descriptor)
    {
        Debug.WriteLine($"OnDeviceListChangedAsync: Enter: {descriptor?.ToString() ?? "(null)"}");

        // Use selected device.
        if (descriptor is { })
        {
            // Or, you could choice from device descriptor:
            CharacteristicsList.Clear();
            foreach (var characteristics in descriptor.Characteristics)
            {
                if (characteristics.PixelFormat != PixelFormats.Unknown)
                {
                    CharacteristicsList.Add(characteristics);
                }
            }

            Characteristics = CharacteristicsList.FirstOrDefault();
        }
        else
        {
            CharacteristicsList.Clear();
            Characteristics = null;
        }

        Debug.WriteLine($"OnDeviceListChangedAsync: Leave: {descriptor?.ToString() ?? "(null)"}");
        return default;
    }

    // Characteristics combo box was changed.
    [PropertyChanged(nameof(Characteristics))]
    private async ValueTask OnCharacteristicsChangedAsync(VideoCharacteristics? characteristics)
    {
        Debug.WriteLine($"OnCharacteristicsChangedAsync: Enter: {characteristics?.ToString() ?? "(null)"}");

        IsEnbaled = false;
        try
        {
            // Close when already opened.
            if (this.captureDevice is { } captureDevice)
            {
                this.captureDevice = null;

                Debug.WriteLine($"OnCharacteristicsChangedAsync: Stopping: {captureDevice.Name}");
                await captureDevice.StopAsync();

                Debug.WriteLine($"OnCharacteristicsChangedAsync: Disposing: {captureDevice.Name}");
                await captureDevice.DisposeAsync();
            }

            // Erase preview.
            Image = null;
            Statistics1 = null;
            Statistics2 = null;
            countFrames = 0;

            // Descriptor is assigned and set valid characteristics:
            if (Device is { } descriptor &&
                characteristics is { })
            {
                // Open capture device:
                Debug.WriteLine($"OnCharacteristicsChangedAsync: Opening: {descriptor.Name}");
                this.captureDevice = await descriptor.OpenAsync(characteristics, OnPixelBufferArrivedAsync);

                // Start capturing.
                Debug.WriteLine($"OnCharacteristicsChangedAsync: Starting: {descriptor.Name}");
                await this.captureDevice.StartAsync();
            }
        }
        finally
        {
            IsEnbaled = true;

            Debug.WriteLine($"OnCharacteristicsChangedAsync: Leave: {characteristics?.ToString() ?? "(null)"}");
        }
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
            Statistics1 = $"Frame={countFrames}/{frameIndex}";
            Statistics2 = $"FPS={realFps:F3} |  Avg{avgFps:F3}";
            Statistics3 = $"SKBitmap={bitmap.Width}x{bitmap.Height} [{bitmap.ColorType}]";
        }
    }
}

