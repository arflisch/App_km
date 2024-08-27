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
    
        // Supposons qu'il n'y a qu'un seul objet Statistic
        var statistic = await _summary.GetStatisticAsync();

        if (statistic != null)
        {
            var rides = statistic.Rides.OrderByDescending(r => r.Id).ToList();
            if (rides.Count == 0)
            {
                Console.WriteLine("No rides found.");
            }
            else
            {
                for (int i = 0; i < rides.Count; i++)
                {
                    Console.WriteLine($"{rides[i].Id}, {rides[i].Distance}");
                }
            }

            var rideViewModels = new ObservableCollection<RidesPage.RideViewModel>();

            foreach (var ride in rides)
            {
                rideViewModels.Add(new RidesPage.RideViewModel
                {
                    Id = ride.Id.ToString(),
                    Distance = $"{ride.Distance:F2} km",
                    Duration = TimeSpan.FromSeconds(ride.Duration).ToString(@"hh\:mm\:ss")
                });
            }

            RidesListView.ItemsSource = rideViewModels;

        }
        else
        {
            Console.WriteLine("Statistic object is null.");
        }
    }

    public class RideViewModel
    {
        public string? Id { get; set; }
        public string? Distance { get; set; }
        public string? Duration { get; set; }
    }
}