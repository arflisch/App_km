using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Car_kilometer.ViewModels;

/// <summary>The rides of one month, shown under a header in the history.</summary>
public sealed class RideMonthGroup : ObservableCollection<RideItem>
{
    public RideMonthGroup(DateTime month, IEnumerable<RideItem> rides) : base(rides)
    {
        Title = Format.Month(month);
        UpdateSummary();
    }

    public string Title { get; }

    public string Summary { get; private set; } = string.Empty;

    public void UpdateSummary()
    {
        Summary = $"{Format.Km(this.Sum(r => r.DistanceKm))} · {Format.RideCount(Count)}";
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Summary)));
    }
}

public sealed partial class HistoryViewModel : ObservableObject
{
    readonly RideRepository _repository;
    readonly DialogService _dialogs;
    bool _isDirty = true;

    public HistoryViewModel(RideRepository repository, DialogService dialogs)
    {
        _repository = repository;
        _dialogs = dialogs;
        _repository.Changed += OnRidesChanged;
    }

    [ObservableProperty] public partial ObservableCollection<RideMonthGroup> Groups { get; set; } = [];
    [ObservableProperty] public partial string Summary { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsEmpty { get; set; }
    [ObservableProperty] public partial bool HasRides { get; set; }

    public async Task OnAppearingAsync()
    {
        if (!_isDirty)
            return;

        _isDirty = false;
        var rides = await _repository.GetRidesAsync();
        Groups = new ObservableCollection<RideMonthGroup>(rides
            .GroupBy(r => new DateTime(r.LocalDate.Year, r.LocalDate.Month, 1))
            .Select(month => new RideMonthGroup(month.Key, month)));
        UpdateSummary();
    }

    [RelayCommand]
    Task Export() => Shell.Current.GoToAsync("export");

    [RelayCommand]
    Task NewRide() => Shell.Current.GoToAsync("//ride");

    public Task OpenRideAsync(RideItem ride) =>
        Shell.Current.GoToAsync("rideeditor", new ShellNavigationQueryParameters { ["Ride"] = ride });

    public async Task DeleteAsync(RideItem ride)
    {
        if (await _dialogs.ConfirmAsync(AppResources.DeleteRideTitle, AppResources.DeleteRideMessage, AppResources.Delete))
            await _repository.DeleteAsync(ride.Id);
    }

    // Edits and deletions are applied in place, so the list keeps its scroll position.
    void OnRidesChanged(object? sender, RideChangedEventArgs e)
    {
        if (e.Change == RideChange.Added)
        {
            _isDirty = true;
            return;
        }

        foreach (var group in Groups)
        {
            var index = IndexOf(group, e.Id);
            if (index < 0)
                continue;

            if (e.Change == RideChange.Updated && e.Ride is { } updated)
            {
                group[index] = updated;
            }
            else
            {
                group.RemoveAt(index);
                if (group.Count == 0)
                    Groups.Remove(group);
                else
                    group.UpdateSummary();
            }
            break;
        }
        UpdateSummary();
    }

    void UpdateSummary()
    {
        var rides = Groups.SelectMany(g => g).ToList();
        IsEmpty = rides.Count == 0;
        HasRides = rides.Count > 0;
        Summary = $"{Format.RideCount(rides.Count)} · {Format.Km(rides.Sum(r => r.DistanceKm))}";
    }

    static int IndexOf(RideMonthGroup group, ObjectId id)
    {
        for (int i = 0; i < group.Count; i++)
        {
            if (group[i].Id == id)
                return i;
        }
        return -1;
    }
}
