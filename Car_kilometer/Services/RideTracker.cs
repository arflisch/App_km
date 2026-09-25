using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Locations;

namespace Car_kilometer.Services;

public enum TrackingState { Idle, Recording, Paused }

public enum StartResult { Started, PermissionDenied, LocationDisabled, Failed }

/// <summary>Values shown while recording. Read by the ride page once per second.</summary>
public readonly record struct TrackingSnapshot(
    TrackingState State,
    double DistanceKm,
    TimeSpan Elapsed,
    double SpeedKmh,
    double MaxSpeedKmh,
    double? AccuracyMeters,
    bool HasFix);

/// <summary>
/// Records a ride: drives the GPS listener, measures the elapsed time and adds up the distance.
/// GPS readings arrive on a background thread (also when the phone is locked), the UI reads <see cref="GetSnapshot"/>.
/// </summary>
public sealed class RideTracker
{
    // Readings less precise than this are not used for the distance.
    const double MaxAccuracyMeters = 35;
    // Moves shorter than this are GPS noise, e.g. while waiting at a red light.
    const double MinStepMeters = 8;
    // Faster than ~250 km/h between two readings can only be a GPS glitch.
    const double MaxPlausibleSpeed = 70;
    // After that many rejected jumps in a row, the previous position was the wrong one: start again from the new one.
    const int MaxRejectedJumps = 5;
    // How often the ride in progress is saved, so it can be recovered if the app is killed.
    static readonly TimeSpan PersistInterval = TimeSpan.FromSeconds(15);
    const string InterruptedRideKey = "tracker.interruptedRide";

    readonly IGpsManager _gps;
    readonly ILogger<RideTracker> _logger;
    readonly Lock _sync = new();
    readonly Stopwatch _elapsed = new();

    Position? _anchor;
    DateTimeOffset _anchorTime;
    int _rejectedJumps;
    double _distanceMeters;
    double _speed;
    double _maxSpeed;
    double? _accuracy;
    DateTimeOffset _lastReadingAt;
    DateTimeOffset _lastPersistedAt;
    DateTimeOffset _startedAt;
    Position? _start;
    Position? _end;

    public RideTracker(IGpsManager gps, ILogger<RideTracker> logger)
    {
        _gps = gps;
        _logger = logger;
    }

    public TrackingState State { get; private set; }

    /// <summary>Raised on the calling (UI) thread when <see cref="State"/> changes.</summary>
    public event EventHandler? StateChanged;

    public async Task<StartResult> StartAsync()
    {
        if (State != TrackingState.Idle)
            return StartResult.Started;

        AccessState access;
        try
        {
            access = await _gps.RequestAccess(PermissionRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Location permission request failed");
            return StartResult.Failed;
        }

        if (access == AccessState.Disabled)
            return StartResult.LocationDisabled;
        if (access is not (AccessState.Available or AccessState.Restricted))
            return StartResult.PermissionDenied;

        lock (_sync)
        {
            _distanceMeters = _speed = _maxSpeed = 0;
            _accuracy = null;
            _anchor = _start = _end = null;
            _rejectedJumps = 0;
            _lastReadingAt = default;
            _startedAt = DateTimeOffset.Now;
            _elapsed.Reset();
        }

        if (!await TryStartGpsAsync())
            return StartResult.Failed;

        lock (_sync)
        {
            _elapsed.Start();
            State = TrackingState.Recording;
        }
        StateChanged?.Invoke(this, EventArgs.Empty);
        Persist();
        return StartResult.Started;
    }

    public async Task PauseAsync()
    {
        if (State != TrackingState.Recording)
            return;

        lock (_sync)
        {
            _elapsed.Stop();
            _speed = 0;
            // The next reading after resuming starts a new segment: what happens during the pause is not counted.
            _anchor = null;
            State = TrackingState.Paused;
        }
        StateChanged?.Invoke(this, EventArgs.Empty);
        Persist();
        await StopGpsAsync();
    }

    public async Task<bool> ResumeAsync()
    {
        if (State != TrackingState.Paused)
            return true;

        if (!await TryStartGpsAsync())
            return false;

        lock (_sync)
        {
            _anchor = null;
            _elapsed.Start();
            State = TrackingState.Recording;
        }
        StateChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Stops recording (the ride stays paused until it is saved or discarded) and returns what was recorded.</summary>
    public async Task<RideDraft> FinishAsync()
    {
        await PauseAsync();
        lock (_sync)
        {
            return new RideDraft(_distanceMeters / 1000, _elapsed.Elapsed, _startedAt, ToLatLon(_start), ToLatLon(_end));
        }
    }

    /// <summary>Forgets the current ride, once it was saved or discarded.</summary>
    public void Reset()
    {
        var wasRecording = State == TrackingState.Recording;
        lock (_sync)
        {
            _elapsed.Reset();
            _distanceMeters = _speed = _maxSpeed = 0;
            _accuracy = null;
            _anchor = _start = _end = null;
            State = TrackingState.Idle;
        }
        Preferences.Default.Remove(InterruptedRideKey);
        StateChanged?.Invoke(this, EventArgs.Empty);
        if (wasRecording)
            _ = StopGpsAsync();
    }

    public TrackingSnapshot GetSnapshot()
    {
        lock (_sync)
        {
            var recentFix = _lastReadingAt != default && DateTimeOffset.UtcNow - _lastReadingAt < TimeSpan.FromSeconds(10);
            return new TrackingSnapshot(
                State,
                _distanceMeters / 1000,
                _elapsed.Elapsed,
                recentFix ? _speed * 3.6 : 0,
                _maxSpeed * 3.6,
                recentFix ? _accuracy : null,
                recentFix);
        }
    }

    /// <summary>A ride that was being recorded when the app was killed, if any.</summary>
    public RideDraft? GetInterruptedRide()
    {
        if (State != TrackingState.Idle)
            return null;

        var saved = Preferences.Default.Get(InterruptedRideKey, string.Empty);
        var parts = saved.Split(';');
        if (parts.Length == 3
            && double.TryParse(parts[0], CultureInfo.InvariantCulture, out var km)
            && double.TryParse(parts[1], CultureInfo.InvariantCulture, out var seconds)
            && DateTimeOffset.TryParse(parts[2], CultureInfo.InvariantCulture, out var startedAt)
            && km >= 0.05)
        {
            return new RideDraft(km, TimeSpan.FromSeconds(seconds), startedAt);
        }

        Preferences.Default.Remove(InterruptedRideKey);
        return null;
    }

    public void ForgetInterruptedRide() => Preferences.Default.Remove(InterruptedRideKey);

    /// <summary>Called by <see cref="RideGpsDelegate"/> for every GPS reading, on a background thread.</summary>
    public void OnReading(GpsReading reading)
    {
        var now = DateTimeOffset.UtcNow;
        bool persist;
        lock (_sync)
        {
            if (State != TrackingState.Recording)
                return;

            Accumulate(reading, now);
            persist = now - _lastPersistedAt > PersistInterval;
        }

        if (persist)
            Persist();
    }

    void Accumulate(GpsReading reading, DateTimeOffset now)
    {
        _lastReadingAt = now;
        _accuracy = reading.PositionAccuracy > 0 ? reading.PositionAccuracy : null;
        // The speed measured by the GPS chip (m/s), negative when unknown
        _speed = reading.IsStationary || reading.Speed < 0.5 ? 0 : reading.Speed;

        if (reading.PositionAccuracy <= 0 || reading.PositionAccuracy > MaxAccuracyMeters)
            return;

        if (_speed < MaxPlausibleSpeed)
            _maxSpeed = Math.Max(_maxSpeed, _speed);

        var position = reading.Position;
        _start ??= position;
        if (_anchor is null)
        {
            MoveAnchor(position, now);
            return;
        }

        // Standing still: keep the anchor, the distance is counted from it once the car moves again.
        if (reading.IsStationary)
            return;

        var step = _anchor.GetDistanceTo(position).TotalMeters;
        if (step < Math.Max(MinStepMeters, reading.PositionAccuracy))
            return;

        var seconds = (now - _anchorTime).TotalSeconds;
        if (seconds > 0 && step / seconds > MaxPlausibleSpeed)
        {
            if (++_rejectedJumps >= MaxRejectedJumps)
                MoveAnchor(position, now);
            return;
        }

        _distanceMeters += step;
        MoveAnchor(position, now);
    }

    void MoveAnchor(Position position, DateTimeOffset time)
    {
        _anchor = _end = position;
        _anchorTime = time;
        _rejectedJumps = 0;
    }

    void Persist()
    {
        string value;
        lock (_sync)
        {
            _lastPersistedAt = DateTimeOffset.UtcNow;
            value = string.Join(';',
                (_distanceMeters / 1000).ToString("R", CultureInfo.InvariantCulture),
                _elapsed.Elapsed.TotalSeconds.ToString("R", CultureInfo.InvariantCulture),
                _startedAt.ToString("O", CultureInfo.InvariantCulture));
        }
        Preferences.Default.Set(InterruptedRideKey, value);
    }

    async Task<bool> TryStartGpsAsync()
    {
        try
        {
            if (_gps.CurrentListener is not null)
                await _gps.StopListener();

            await _gps.StartListener(ListenRequest);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not start the GPS listener");
            return false;
        }
    }

    async Task StopGpsAsync()
    {
        try
        {
            await _gps.StopListener();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not stop the GPS listener");
        }
    }

    static LatLon? ToLatLon(Position? position) =>
        position is null ? null : new LatLon(position.Latitude, position.Longitude);

#if ANDROID
    // Realtime runs a foreground service, so the ride keeps being recorded with the screen off.
    static GpsRequest ListenRequest => new AndroidGpsRequest(
        BackgroundMode: GpsBackgroundMode.Realtime,
        GpsPriority: GpsPriority.HighAccuracy,
        IntervalMillis: 1000,
        RequestPreciseAccuracy: true,
        AutoRestart: false);

    static GpsRequest PermissionRequest => ListenRequest;
#elif IOS
    // Realtime keeps a background activity session open, so the ride keeps being recorded with the screen locked.
    static GpsRequest ListenRequest => new AppleGpsRequest(
        BackgroundMode: GpsBackgroundMode.Realtime,
        RequestPreciseAccuracy: true,
        AutoRestart: false,
        PausesLocationUpdatesAutomatically: false,
        ActivityType: CoreLocation.CLActivityType.AutomotiveNavigation);

    // "While using the app" is enough: recording always starts in the foreground.
    static GpsRequest PermissionRequest => new(GpsBackgroundMode.None);
#else
    static GpsRequest ListenRequest => new(GpsBackgroundMode.None, RequestPreciseAccuracy: true, AutoRestart: false);

    static GpsRequest PermissionRequest => ListenRequest;
#endif
}
