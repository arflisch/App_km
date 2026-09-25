using System.Globalization;
using Car_kilometer.Resources;

namespace Car_kilometer.ViewModels;

public sealed partial class RideViewModel : ObservableObject
{
    readonly RideTracker _tracker;
    readonly DialogService _dialogs;

    public RideViewModel(RideTracker tracker, DialogService dialogs)
    {
        _tracker = tracker;
        _dialogs = dialogs;
        _tracker.StateChanged += (_, _) => Refresh();
        Refresh();
    }

    [ObservableProperty] public partial TrackingState State { get; set; }
    [ObservableProperty] public partial bool IsIdle { get; set; } = true;
    [ObservableProperty] public partial bool IsRecording { get; set; }
    [ObservableProperty] public partial bool IsPaused { get; set; }
    [ObservableProperty] public partial bool IsActive { get; set; }
    [ObservableProperty] public partial double Speed { get; set; }
    [ObservableProperty] public partial string SpeedText { get; set; } = "0";
    [ObservableProperty] public partial string DistanceText { get; set; } = "0";
    [ObservableProperty] public partial string ElapsedText { get; set; } = "00:00:00";
    [ObservableProperty] public partial string AverageSpeedText { get; set; } = "0";
    [ObservableProperty] public partial string MaxSpeedText { get; set; } = "0";
    [ObservableProperty] public partial string StatusText { get; set; } = string.Empty;
    [ObservableProperty] public partial string GpsGlyph { get; set; } = Icons.GpsFixed;
    [ObservableProperty] public partial string GpsText { get; set; } = string.Empty;

    /// <summary>Reads the tracker. Called by the page twice a second while it is visible.</summary>
    public void Refresh()
    {
        var snapshot = _tracker.GetSnapshot();
        IsIdle = snapshot.State == TrackingState.Idle;
        IsRecording = snapshot.State == TrackingState.Recording;
        IsPaused = snapshot.State == TrackingState.Paused;
        IsActive = !IsIdle;
        // Last, so the flags above are up to date when the page reacts to the new state
        State = snapshot.State;

        Speed = snapshot.SpeedKmh;
        SpeedText = Format.Number(snapshot.SpeedKmh, 0);
        DistanceText = Format.Number(snapshot.DistanceKm, snapshot.DistanceKm < 100 ? 2 : 1);
        ElapsedText = Format.Clock(snapshot.Elapsed);
        var hours = snapshot.Elapsed.TotalHours;
        AverageSpeedText = Format.Number(hours > 0.001 ? snapshot.DistanceKm / hours : 0, 0);
        MaxSpeedText = Format.Number(snapshot.MaxSpeedKmh, 0);

        StatusText = snapshot.State switch
        {
            TrackingState.Recording => AppResources.StatusRecording,
            TrackingState.Paused => AppResources.StatusPaused,
            _ => AppResources.StatusReady,
        };

        (GpsGlyph, GpsText) = snapshot switch
        {
            { State: not TrackingState.Recording } => (Icons.GpsFixed, AppResources.GpsIdle),
            { HasFix: true, AccuracyMeters: { } accuracy } => (Icons.GpsFixed, string.Format(CultureInfo.CurrentCulture, AppResources.GpsAccuracy, Math.Round(accuracy))),
            _ => (Icons.GpsSearching, AppResources.GpsSearching),
        };
    }

    [RelayCommand]
    async Task Start()
    {
        Haptics();
        var result = await _tracker.StartAsync();
        Refresh();

        switch (result)
        {
            case StartResult.PermissionDenied:
                if (await _dialogs.ConfirmAsync(AppResources.LocationDeniedTitle, AppResources.LocationDeniedMessage, AppResources.OpenSettings))
                    AppInfo.Current.ShowSettingsUI();
                break;
            case StartResult.LocationDisabled:
                await _dialogs.AlertAsync(AppResources.LocationDisabledTitle, AppResources.LocationDisabledMessage);
                break;
            case StartResult.Failed:
                await _dialogs.AlertAsync(AppResources.ErrorTitle, AppResources.GpsStartError);
                break;
        }
    }

    [RelayCommand]
    async Task Pause()
    {
        Haptics();
        await _tracker.PauseAsync();
        Refresh();
    }

    [RelayCommand]
    async Task Resume()
    {
        Haptics();
        if (!await _tracker.ResumeAsync())
            await _dialogs.AlertAsync(AppResources.ErrorTitle, AppResources.GpsStartError);
        Refresh();
    }

    [RelayCommand]
    async Task Finish()
    {
        Haptics();
        var draft = await _tracker.FinishAsync();
        Refresh();
        await Shell.Current.GoToAsync("rideeditor", new ShellNavigationQueryParameters { ["Draft"] = draft });
    }

    static void Haptics()
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch (Exception)
        {
            // Haptics are a nicety: some devices have no vibrator.
        }
    }
}
