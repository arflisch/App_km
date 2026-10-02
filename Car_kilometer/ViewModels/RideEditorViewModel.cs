namespace Car_kilometer.ViewModels;

public sealed partial class WeatherOption(Weather weather) : ObservableObject
{
    public Weather Weather { get; } = weather;
    public string Label { get; } = WeatherInfo.Label(weather);
    public string Glyph { get; } = WeatherInfo.Glyph(weather);

    [ObservableProperty] public partial bool IsSelected { get; set; }
}

public sealed partial class GuideOption(string name) : ObservableObject
{
    public string Name { get; } = name;

    [ObservableProperty] public partial bool IsSelected { get; set; }
}

public sealed partial class RoadTypeOption(RoadType type) : ObservableObject
{
    public RoadType Type { get; } = type;
    public string Label { get; } = RoadTypeInfo.Label(type);
    public string Glyph { get; } = RoadTypeInfo.Glyph(type);

    [ObservableProperty] public partial bool IsSelected { get; set; }
}

/// <summary>Saves a ride that was just recorded (query "Draft"), or edits a saved one (query "Ride").</summary>
public sealed partial class RideEditorViewModel : ObservableObject, IQueryAttributable
{
    readonly RideRepository _repository;
    readonly RideTracker _tracker;
    readonly PlaceNameService _places;
    readonly DialogService _dialogs;
    readonly UserSettings _settings;
    RideDraft? _draft;
    RideItem? _ride;

    public RideEditorViewModel(RideRepository repository, RideTracker tracker, PlaceNameService places, DialogService dialogs, UserSettings settings)
    {
        _repository = repository;
        _tracker = tracker;
        _places = places;
        _dialogs = dialogs;
        _settings = settings;
    }

    public IReadOnlyList<WeatherOption> WeatherOptions { get; } = WeatherInfo.Choices.Select(w => new WeatherOption(w)).ToArray();

    public IReadOnlyList<RoadTypeOption> RoadTypeOptions { get; } = RoadTypeInfo.Choices.Select(t => new RoadTypeOption(t)).ToArray();

    // Roadbook: shown when the mode is on, or when editing a ride that has roadbook data
    [ObservableProperty] public partial bool IsRoadbook { get; set; }
    [ObservableProperty] public partial IReadOnlyList<GuideOption> GuideOptions { get; set; } = [];
    [ObservableProperty] public partial string SelectedGuide { get; set; } = string.Empty;

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
            IsRoadbook = _settings.RoadbookEnabled;
            var guides = _settings.Guides;
            SetGuides(guides, guides.Count == 1 ? guides[0] : guides.FirstOrDefault(g => g == _settings.LastGuide) ?? string.Empty);
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
            IsRoadbook = _settings.RoadbookEnabled || ride.Guide.Length > 0 || ride.RoadTypes != RoadType.None;
            SetGuides(_settings.Guides, ride.Guide);
            foreach (var option in RoadTypeOptions)
                option.IsSelected = ride.RoadTypes.HasFlag(option.Type);
        }
    }

    public void SelectGuide(GuideOption option) => SelectedGuide = option.Name;

    public void ToggleRoadType(RoadTypeOption option) => option.IsSelected = !option.IsSelected;

    partial void OnSelectedGuideChanged(string value)
    {
        Error = null;
        foreach (var option in GuideOptions)
            option.IsSelected = option.Name == value;
    }

    void SetGuides(IReadOnlyList<string> guides, string selected)
    {
        // Keep the guide of an older ride selectable even if the settings changed since
        var names = selected.Length > 0 && !guides.Contains(selected) ? [.. guides, selected] : guides;
        GuideOptions = names.Select(name => new GuideOption(name)).ToArray();
        SelectedGuide = selected;
        OnSelectedGuideChanged(selected);
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
        if (_draft is not null && IsRoadbook && GuideOptions.Count > 0 && SelectedGuide.Length == 0)
        {
            Error = AppResources.GuideChoiceRequired;
            return;
        }

        var roadTypes = RoadTypeOptions.Where(o => o.IsSelected).Aggregate(RoadType.None, (types, o) => types | o.Type);
        if (_draft is { } draft)
        {
            // Read before saving: once added, the Ride belongs to a Realm instance that is closed right after
            var guide = IsRoadbook ? SelectedGuide : string.Empty;
            var ride = new Ride(description, Math.Round(draft.DistanceKm, 3), draft.Duration, draft.StartedAt, WeatherInfo.ToStored(SelectedWeather))
            {
                Guide = guide,
                RoadTypes = IsRoadbook ? (int)roadTypes : 0,
            };
            await _repository.AddAsync(ride);
            if (guide.Length > 0)
                _settings.LastGuide = guide;
            _tracker.Reset();
        }
        else if (_ride is { } ride)
        {
            // Keep what was stored when no weather is picked (rides from the first versions have none)
            var weather = SelectedWeather == Weather.Unknown ? ride.WeatherCondition : WeatherInfo.ToStored(SelectedWeather);
            var guide = IsRoadbook ? SelectedGuide : ride.Guide;
            await _repository.UpdateAsync(ride.Id, description, weather, guide, IsRoadbook ? roadTypes : ride.RoadTypes);
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
