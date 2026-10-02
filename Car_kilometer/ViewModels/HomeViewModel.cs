using System.Globalization;

namespace Car_kilometer.ViewModels;

/// <summary>One bar of the "last 6 months" chart.</summary>
public sealed record MonthBar(string Label, string ValueText, double BarHeight, bool HasValue, bool IsCurrent);

public sealed partial class HomeViewModel : ObservableObject
{
    const double ChartHeight = 88;

    readonly RideRepository _repository;
    readonly RideTracker _tracker;
    readonly UserSettings _settings;
    readonly DialogService _dialogs;
    bool _isDirty = true;
    bool _interruptedRideChecked;
    IReadOnlyList<RideItem> _rides = [];

    public HomeViewModel(RideRepository repository, RideTracker tracker, UserSettings settings, DialogService dialogs)
    {
        _repository = repository;
        _tracker = tracker;
        _settings = settings;
        _dialogs = dialogs;
        _repository.Changed += (_, _) => _isDirty = true;
        _tracker.StateChanged += (_, _) => IsRecording = _tracker.State != TrackingState.Idle;
    }

    [ObservableProperty] public partial string Greeting { get; set; } = string.Empty;
    [ObservableProperty] public partial string Today { get; set; } = string.Empty;
    [ObservableProperty] public partial string TotalDistance { get; set; } = "0";
    [ObservableProperty] public partial string TotalRides { get; set; } = "0";
    [ObservableProperty] public partial string TotalTime { get; set; } = string.Empty;
    [ObservableProperty] public partial string MonthDistance { get; set; } = string.Empty;
    [ObservableProperty] public partial string MonthDetails { get; set; } = string.Empty;
    [ObservableProperty] public partial IReadOnlyList<MonthBar> MonthBars { get; set; } = [];
    [ObservableProperty] public partial IReadOnlyList<RideItem> RecentRides { get; set; } = [];
    [ObservableProperty] public partial bool HasRides { get; set; }
    [ObservableProperty] public partial bool IsEmpty { get; set; }
    [ObservableProperty] public partial bool IsRecording { get; set; }

    // Roadbook card (learner drivers), or an invitation to turn the mode on
    [ObservableProperty] public partial bool IsRoadbookEnabled { get; set; }
    [ObservableProperty] public partial bool ShowRoadbookInvite { get; set; }
    [ObservableProperty] public partial string RoadbookKm { get; set; } = string.Empty;
    [ObservableProperty] public partial double RoadbookRatio { get; set; }
    [ObservableProperty] public partial string RoadbookDetails { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsRoadbookComplete { get; set; }

    public async Task OnAppearingAsync()
    {
        var now = DateTime.Now;
        Today = Format.Today(now);
        var greeting = now.Hour switch
        {
            >= 5 and < 12 => AppResources.GreetingMorning,
            >= 12 and < 18 => AppResources.GreetingAfternoon,
            _ => AppResources.GreetingEvening,
        };
        var firstName = _settings.FirstName;
        Greeting = string.IsNullOrEmpty(firstName) ? greeting : string.Format(CultureInfo.CurrentCulture, AppResources.GreetingWithName, greeting, firstName);
        IsRecording = _tracker.State != TrackingState.Idle;

        if (_isDirty)
        {
            _isDirty = false;
            _rides = await _repository.GetRidesAsync();
            Update(_rides, now);
        }
        // Settings may have changed in the roadbook sheet: cheap to recompute every time
        UpdateRoadbook();

        if (!_interruptedRideChecked)
        {
            _interruptedRideChecked = true;
            await OfferInterruptedRideAsync();
        }
    }

    [RelayCommand]
    Task NewRide() => Shell.Current.GoToAsync("//ride");

    [RelayCommand]
    Task Export() => Shell.Current.GoToAsync("export");

    [RelayCommand]
    Task ShowHistory() => Shell.Current.GoToAsync("//history");

    [RelayCommand]
    Task OpenRoadbook() => Shell.Current.GoToAsync("roadbook");

    public Task OpenRideAsync(RideItem ride) =>
        Shell.Current.GoToAsync("rideeditor", new ShellNavigationQueryParameters { ["Ride"] = ride });

    void Update(IReadOnlyList<RideItem> rides, DateTime now)
    {
        TotalDistance = Format.Number(rides.Sum(r => r.DistanceKm));
        TotalRides = rides.Count.ToString("N0", CultureInfo.CurrentCulture);
        TotalTime = Format.Duration(TimeSpan.FromTicks(rides.Sum(r => r.Duration.Ticks)));
        HasRides = rides.Count > 0;
        IsEmpty = rides.Count == 0;
        RecentRides = rides.Take(3).ToArray();

        // Kilometers of the last 6 months, the current one last
        var firstMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-5);
        var kilometers = new double[6];
        var rideCount = 0;
        var thisMonthTime = TimeSpan.Zero;
        foreach (var ride in rides)
        {
            var date = ride.LocalDate;
            var index = (date.Year - firstMonth.Year) * 12 + date.Month - firstMonth.Month;
            if (index is < 0 or > 5)
                continue;

            kilometers[index] += ride.DistanceKm;
            if (index == 5)
            {
                rideCount++;
                thisMonthTime += ride.Duration;
            }
        }

        var max = Math.Max(kilometers.Max(), 1);
        MonthBars = kilometers
            .Select((km, i) => new MonthBar(
                Format.MonthShort(firstMonth.AddMonths(i)),
                Format.Number(km, 0),
                Math.Max(8, km / max * ChartHeight),
                km > 0,
                i == 5))
            .ToArray();

        MonthDistance = Format.Number(kilometers[5]);
        MonthDetails = $"{Format.RideCount(rideCount)} · {Format.Duration(thisMonthTime)}";
    }

    void UpdateRoadbook()
    {
        IsRoadbookEnabled = _settings.RoadbookEnabled;
        ShowRoadbookInvite = !IsRoadbookEnabled;
        if (!IsRoadbookEnabled)
            return;

        var progress = Roadbook.Progress(_rides, _settings);
        var culture = CultureInfo.CurrentCulture;
        RoadbookKm = string.Format(culture, AppResources.RoadbookProgress, Format.Number(progress.Km), Format.Number(progress.GoalKm, 0));
        RoadbookRatio = progress.Ratio;
        IsRoadbookComplete = progress.IsComplete;
        RoadbookDetails = progress.IsComplete
            ? string.Format(culture, AppResources.RoadbookDone, Format.Date(progress.Start))
            : string.Format(culture, AppResources.RoadbookRemaining, Format.Km(progress.RemainingKm), Format.Date(progress.Start));
    }

    async Task OfferInterruptedRideAsync()
    {
        if (_tracker.GetInterruptedRide() is not { } draft)
            return;

        var save = await _dialogs.ConfirmAsync(
            AppResources.InterruptedRideTitle,
            string.Format(CultureInfo.CurrentCulture, AppResources.InterruptedRideMessage, Format.Km(draft.DistanceKm)),
            AppResources.Save,
            AppResources.Discard);

        if (save)
            await Shell.Current.GoToAsync("rideeditor", new ShellNavigationQueryParameters { ["Draft"] = draft });
        else
            _tracker.ForgetInterruptedRide();
    }
}
