namespace Car_kilometer.ViewModels;

public sealed partial class WeatherOption(Weather weather) : ObservableObject
{
    public Weather Weather { get; } = weather;
    public string Label { get; } = WeatherInfo.Label(weather);
    public string Glyph { get; } = WeatherInfo.Glyph(weather);

    [ObservableProperty] public partial bool IsSelected { get; set; }
}

/// <summary>Saves a ride that was just recorded (query "Draft"), or edits a saved one (query "Ride").</summary>
public sealed partial class RideEditorViewModel : ObservableObject, IQueryAttributable
{
    readonly RideRepository _repository;
    readonly RideTracker _tracker;
    readonly PlaceNameService _places;
    readonly DialogService _dialogs;
    RideDraft? _draft;
    RideItem? _ride;

    public RideEditorViewModel(RideRepository repository, RideTracker tracker, PlaceNameService places, DialogService dialogs)
    {
        _repository = repository;
        _tracker = tracker;
        _places = places;
        _dialogs = dialogs;
    }

    public IReadOnlyList<WeatherOption> WeatherOptions { get; } = WeatherInfo.Choices.Select(w => new WeatherOption(w)).ToArray();

    [ObservableProperty] public partial string Title { get; set; } = string.Empty;
    [ObservableProperty] public partial string DiscardText { get; set; } = string.Empty;
    [ObservableProperty] public partial string DistanceText { get; set; } = string.Empty;
    [ObservableProperty] public partial string DurationText { get; set; } = string.Empty;
    [ObservableProperty] public partial string SpeedText { get; set; } = string.Empty;
    [ObservableProperty] public partial string DateText { get; set; } = string.Empty;
    [ObservableProperty] public partial string Description { get; set; } = string.Empty;
    [ObservableProperty] public partial Weather SelectedWeather { get; set; }
    [ObservableProperty] public partial bool IsLookingUpPlace { get; set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? Error { get; set; }

    public bool HasError => Error is not null;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Draft", out var value) && value is RideDraft draft)
        {
            _draft = draft;
            Title = AppResources.SaveRideTitle;
            DiscardText = AppResources.DiscardRide;
            Show(draft.DistanceKm, draft.Duration, draft.StartedAt.LocalDateTime);
            _ = SuggestDescriptionAsync(draft);
        }
        else if (query.TryGetValue("Ride", out value) && value is RideItem ride)
        {
            _ride = ride;
            Title = AppResources.EditRideTitle;
            DiscardText = AppResources.DeleteRide;
            Show(ride.DistanceKm, ride.Duration, ride.LocalDate);
            Description = ride.Description;
            SelectedWeather = ride.Weather;
        }
    }

    public void SelectWeather(WeatherOption option) => SelectedWeather = option.Weather;

    partial void OnSelectedWeatherChanged(Weather value)
    {
        foreach (var option in WeatherOptions)
            option.IsSelected = option.Weather == value;
    }

    partial void OnDescriptionChanged(string value) => Error = null;

    [RelayCommand]
    async Task Save()
    {
        var description = Description.Trim();
        if (description.Length == 0)
        {
            Error = AppResources.DescriptionRequired;
            return;
        }
        if (_draft is not null && SelectedWeather == Weather.Unknown)
        {
            Error = AppResources.WeatherRequired;
            return;
        }

        if (_draft is { } draft)
        {
            var ride = new Ride(description, Math.Round(draft.DistanceKm, 3), draft.Duration, draft.StartedAt, WeatherInfo.ToStored(SelectedWeather));
            await _repository.AddAsync(ride);
            _tracker.Reset();
        }
        else if (_ride is { } ride)
        {
            // Keep what was stored when no weather is picked (rides from the first versions have none)
            var weather = SelectedWeather == Weather.Unknown ? ride.WeatherCondition : WeatherInfo.ToStored(SelectedWeather);
            await _repository.UpdateAsync(ride.Id, description, weather);
        }

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    async Task Discard()
    {
        if (_draft is not null)
        {
            if (!await _dialogs.ConfirmAsync(AppResources.DiscardRideTitle, AppResources.DiscardRideMessage, AppResources.Discard))
                return;
            _tracker.Reset();
        }
        else if (_ride is { } ride)
        {
            if (!await _dialogs.ConfirmAsync(AppResources.DeleteRideTitle, AppResources.DeleteRideMessage, AppResources.Delete))
                return;
            await _repository.DeleteAsync(ride.Id);
        }

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    Task Close() => Shell.Current.GoToAsync("..");

    void Show(double distanceKm, TimeSpan duration, DateTime date)
    {
        DistanceText = Format.Number(distanceKm, 2);
        DurationText = Format.Duration(duration);
        SpeedText = Format.Number(duration.TotalHours > 0.001 ? distanceKm / duration.TotalHours : 0, 0);
        DateText = $"{Format.Capitalize(Format.ShortDate(date))} · {Format.Time(date)}";
    }

    async Task SuggestDescriptionAsync(RideDraft draft)
    {
        if (draft.Start is null && draft.End is null)
            return;

        IsLookingUpPlace = true;
        var suggestion = await _places.SuggestDescriptionAsync(draft.Start, draft.End);
        IsLookingUpPlace = false;

        if (!string.IsNullOrEmpty(suggestion) && string.IsNullOrWhiteSpace(Description))
            Description = suggestion;
    }
}
