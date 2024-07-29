namespace Car_kilometer;
using Car_kilometer.Models;
using Car_kilometer.NewFolder;
using Car_kilometer.Services;
using System.Collections.ObjectModel;

public partial class RidesPage : ContentPage
{
    private readonly Summary _summary;
    public RidesPage()
	{
		InitializeComponent();
        _summary = MauiProgram.ServiceProvider.GetRequiredService<Summary>();
        LoadRides();
	}

    private async void LoadRides()
	{
		var realm = await Realm.GetInstanceAsync();
    
        // Supposons qu'il n'y a qu'un seul objet Statistic
        var statistic = realm.All<Statistic>().FirstOrDefault();
    
        if (statistic != null)
        {
            var rides = statistic.Rides.OrderByDescending(r => r.Id).ToList();
            var rideViewModels = new ObservableCollection<RidesPage.RideViewModel>();

            foreach (var ride in rides)
            {
                rideViewModels.Add(new RidesPage.RideViewModel
                {
                    Id = ride.Id.ToString(),
                    Distance = $"{ride.Distance / 1000:F2} km",
                    Duration = ride.Duration.ToString(@"hh\:mm\:ss")
                });
            }

            RidesListView.ItemsSource = rideViewModels;
        }
    }

    public class RideViewModel
    {
        public string? Id { get; set; }
        public string? Distance { get; set; }
        public string? Duration { get; set; }
    }
}