using System.Globalization;

namespace Car_kilometer.ViewModels;

/// <summary>Turns the roadbook mode on or off and sets its start date, guides and goal.</summary>
public sealed partial class RoadbookViewModel : ObservableObject
{
    readonly UserSettings _settings;

    public RoadbookViewModel(UserSettings settings)
    {
        _settings = settings;
        IsEnabled = settings.RoadbookEnabled;
        Start = settings.RoadbookStart;
        Guide1 = settings.Guide1;
        Guide2 = settings.Guide2;
        Goal = settings.RoadbookGoalKm.ToString(CultureInfo.CurrentCulture);
    }

    [ObservableProperty] public partial bool IsEnabled { get; set; }
    [ObservableProperty] public partial DateTime Start { get; set; }
    [ObservableProperty] public partial string Guide1 { get; set; } = string.Empty;
    [ObservableProperty] public partial string Guide2 { get; set; } = string.Empty;
    [ObservableProperty] public partial string Goal { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? Error { get; set; }

    public bool HasError => Error is not null;

    partial void OnIsEnabledChanged(bool value) => Error = null;

    [RelayCommand]
    async Task Save()
    {
        if (IsEnabled)
        {
            if (string.IsNullOrWhiteSpace(Guide1) && string.IsNullOrWhiteSpace(Guide2))
            {
                Error = AppResources.GuideRequired;
                return;
            }
            if (!int.TryParse(Goal, NumberStyles.Integer, CultureInfo.CurrentCulture, out var goal) || goal <= 0)
            {
                Error = AppResources.GoalInvalid;
                return;
            }

            _settings.RoadbookStart = Start;
            _settings.RoadbookGoalKm = goal;
            _settings.Guide1 = Guide1;
            _settings.Guide2 = Guide2;
        }

        _settings.RoadbookEnabled = IsEnabled;
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    Task Close() => Shell.Current.GoToAsync("..");
}
