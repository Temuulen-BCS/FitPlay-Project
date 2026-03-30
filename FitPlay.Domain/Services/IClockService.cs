namespace FitPlay.Domain.Services;

/// <summary>
/// Provides a mockable clock abstraction for the entire application.
/// In production, returns DateTime.UtcNow.
/// In dev/test mode, returns a configurable mock time via /api/dev endpoints.
/// </summary>
public interface IClockService
{
    /// <summary>
    /// Returns the current UTC time, or the mocked time if set.
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// Whether a mock time is currently active.
    /// </summary>
    bool IsMocked { get; }

    /// <summary>
    /// Set a mock time. All subsequent calls to UtcNow will return this value.
    /// Pass null to reset to real time.
    /// </summary>
    void SetMockTime(DateTime? utcTime);

    /// <summary>
    /// Reset to real time.
    /// </summary>
    void Reset();
}

public class ClockService : IClockService
{
    private DateTime? _mockTime;

    public DateTime UtcNow => _mockTime ?? DateTime.UtcNow;

    public bool IsMocked => _mockTime.HasValue;

    public void SetMockTime(DateTime? utcTime)
    {
        _mockTime = utcTime.HasValue
            ? DateTime.SpecifyKind(utcTime.Value.ToUniversalTime(), DateTimeKind.Utc)
            : null;
    }

    public void Reset() => _mockTime = null;
}
