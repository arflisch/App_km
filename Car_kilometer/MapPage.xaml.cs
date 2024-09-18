using AndroidX.Browser.Trusted.Sharing;
using Car_kilometer.Models;
using Car_kilometer.NewFolder;
using Car_kilometer.Services;
using Shiny.Locations;
using System.Diagnostics;

namespace Car_kilometer;

public partial class MapPage : ContentPage
{
    private Timer? timer;
    private Stopwatch stopwatch;
    private bool isTimerRunning = false;

    public MapPage()
    {
        InitializeComponent();
        _summary = MauiProgram.ServiceProvider.GetRequiredService<Summary>();
        _gpsManager = MauiProgram.ServiceProvider.GetRequiredService<IGpsManager>();
        _sharedDto = MauiProgram.ServiceProvider.GetRequiredService<SharedDto>();

        // Initialize the Stopwatch
        stopwatch = new Stopwatch();
    }

    private Summary _summary;
    readonly IGpsManager _gpsManager;
    private SharedDto _sharedDto;

    private async void StartButton_Clicked(object sender, EventArgs e)
    {
        if (!isTimerRunning)
        {
            // Start the stopwatch
            stopwatch.Start();
            isTimerRunning = true;

            // Start the timer to update the UI
            timer = new Timer(UpdateUI, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

            // Display appropriate buttons
            stopButton.IsVisible = true;
            pauseButton.IsVisible = true;
            startButton.IsVisible = false;

            // Stop the current GPS listener
            if (_gpsManager.CurrentListener != null)
            {
                await _gpsManager.StopListener();
            }

            // Start a new GPS listener
            await _gpsManager.StartListener(new GpsRequest
            {
                BackgroundMode = GpsBackgroundMode.Realtime,
                Accuracy = GpsAccuracy.Highest,
                //DistanceFilterMeters = 5
            });
        }
    }

    private readonly object _lock = new object();
    private void UpdateUI(object? state)
    {
        Dispatcher.Dispatch(() =>
        {
            // Read values from _sharedDto
            double totalDistance;
            double speed;

            lock (_lock)
            {
                totalDistance = _sharedDto.TotalDistanceDuringRide;
                speed = _sharedDto.Speed;
            }

            // Convert units and update UI
            double totalDistanceKm = (totalDistance / 1000);
            double speedKmH = (speed * 3.6);

            // Get elapsed time from Stopwatch
            var elapsedTime = stopwatch.Elapsed;

            timerLabel.Text = $"{elapsedTime.Hours:00}:{elapsedTime.Minutes:00}:{elapsedTime.Seconds:00}";
            KmLabel.Text = totalDistanceKm.ToString("F1");
            SpeedLabel.Text = speedKmH.ToString("F0");
        });
    }

    private async void StopButton_Clicked(object sender, EventArgs e)
    {
        if (timer != null)
        {
            await _gpsManager.StopListener();
            timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            bool answer = await DisplayAlert("Confirmation", "Do you really stop?", "yes", "No");

            if (answer)
            {
                // Stop the stopwatch
                stopwatch.Stop();

                // Ask for a description
                string description = await DisplayPromptAsync("Description", "Please enter a description for your activity:", "register");

                if (!string.IsNullOrEmpty(description))
                {
                    string weatherCondition = await DisplayActionSheet("Weather Condition", "Cancel", null, "Sunny", "Cloudy", "Rainy", "Windy", "Snowy", "Night");

                    if (weatherCondition != "Cancel")
                    {
                        // Ajouter l'activité avec la description et la météo
                        await Add(description, weatherCondition);

                        // Reset the stopwatch and UI
                        stopwatch.Reset();
                        Dispatcher.Dispatch(() =>
                        {
                            timerLabel.Text = "00:00:00";
                            KmLabel.Text = "0";
                            SpeedLabel.Text = "0";
                        });

                        stopButton.IsVisible = false;
                        pauseButton.IsVisible = false;
                        resumeButton.IsVisible = false;
                        startButton.IsVisible = true;

                        isTimerRunning = false;
                    }
                }
            }
            else
            {
                timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            }
        }
    }

    private async Task Add(string description, string weatherCondition)
    {
        double _distance = _sharedDto.TotalDistanceDuringRide / 1000;
        await _summary.UpdateAsync(stopwatch.Elapsed, _distance, new Ride(description, _distance, stopwatch.Elapsed, DateTime.Now, weatherCondition));
        _sharedDto.TotalDistanceDuringRide = 0;
    }

    private void PauseButton_Clicked(object sender, EventArgs e)
    {
        // Pause the stopwatch
        if (timer != null && isTimerRunning)
        {
            stopwatch.Stop();
            timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            stopButton.IsVisible = false;
            pauseButton.IsVisible = false;
            startButton.IsVisible = false;
            resumeButton.IsVisible = true;

            isTimerRunning = false;
        }
    }

    private void ResumeButton_Clicked(object sender, EventArgs e)
    {
        // Resume the stopwatch
        if (timer != null && !isTimerRunning)
        {
            stopwatch.Start();
            timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));

            stopButton.IsVisible = true;
            pauseButton.IsVisible = true;
            startButton.IsVisible = false;
            resumeButton.IsVisible = false;

            isTimerRunning = true;
        }
    }
}
