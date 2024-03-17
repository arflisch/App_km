using Car_kilometer.Models;
using Car_kilometer.NewFolder;
using Car_kilometer.Services;
using Shiny.Locations;

namespace Car_kilometer;

public partial class MapPage : ContentPage
{
    private Timer timer;
    private TimeSpan elapsedTime;
    private bool isTimerRunning = false;
    private DateTime rideStartTime;

    public MapPage()
    {
        InitializeComponent();
        _summary = MauiProgram.ServiceProvider.GetRequiredService<Summary>();
        _gpsManager = MauiProgram.ServiceProvider.GetRequiredService<IGpsManager>();
        _sharedDto = MauiProgram.ServiceProvider.GetRequiredService<SharedDto>();
    }

    private Summary _summary;

    readonly IGpsManager _gpsManager;
    private SharedDto _sharedDto;
    private async void StartButton_Clicked(object sender, EventArgs e)
    {
        if (!isTimerRunning)
        {
            // D�marrer le trajet
            rideStartTime = DateTime.Now;
            isTimerRunning = true;

            // D�marrer le timer pour mettre � jour l'interface utilisateur
            timer = new Timer(UpdateUI, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

            // Afficher les boutons appropri�s
            stopButton.IsVisible = true;
            pauseButton.IsVisible = true;
            startButton.IsVisible = false;

            // Arr�ter l'�couteur GPS actuel
            if (_gpsManager.CurrentListener != null)
            {
                await _gpsManager.StopListener();
            }

            // D�marrer un nouvel �couteur GPS
            await _gpsManager.StartListener(new GpsRequest
            {
                BackgroundMode = GpsBackgroundMode.Realtime,
                Accuracy = GpsAccuracy.Highest,
                //DistanceFilterMeters = 5
            });
        }
    }


    //Position CurrentPosition;
    //Position PreviousPosition;
    private readonly object _lock = new object();
    private async void UpdateUI(object state)
    {
        Dispatcher.Dispatch(() =>
        {
            // Calculer le temps �coul� depuis le d�but du trajet
            elapsedTime = DateTime.Now - rideStartTime;

            // Lecture des valeurs de _sharedDto
            double totalDistance;
            double speed;

            lock (_lock)
            {
                totalDistance = _sharedDto.TotalDistanceDuringRide;
                speed = _sharedDto.Speed;
            }

            // Conversion des unit�s et mise � jour de l'interface utilisateur
            int totalDistanceKm = (int)(totalDistance / 1000);
            int speedKmH = (int)(speed * 3.6);

            timerLabel.Text = $"{elapsedTime.Hours:00}:{elapsedTime.Minutes:00}:{elapsedTime.Seconds:00}";
            KmLabel.Text = totalDistanceKm.ToString();
            SpeedLabel.Text = speedKmH.ToString();
        });
    }


    private async void StopButton_Clicked(object sender, EventArgs e)
    {
        await _gpsManager.StopListener();
        timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        bool answer = await DisplayAlert("Confirmation", "Do you really stop?", "yes", "No");

        if (answer)
        {
            // L'utilisateur a choisi "Oui", donc arr�ter le chronom�tre et r�initialiser
            timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            stopButton.IsVisible = false;
            pauseButton.IsVisible = false;
            resumeButton.IsVisible = false;
            startButton.IsVisible = true;

            await Add();

            // R�initialiser le chronom�tre et l'interface utilisateur
            elapsedTime = TimeSpan.Zero;
            Dispatcher.Dispatch(() =>
            {
                timerLabel.Text = elapsedTime.ToString(@"hh\:mm\:ss");
                KmLabel.Text = "0";
                SpeedLabel.Text = "0";
            });

            isTimerRunning = false;
        }
        else
        {
            // L'utilisateur a choisi "Non", donc reprendre le chronom�tre
            timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }
    }

    private async Task Add()
    {
        var realm = await Realm.GetInstanceAsync();
        Statistic statistic = await _summary.GetStatisticAsync();

        await realm.WriteAsync(() =>
        {
            realm.Add(new Ride(statistic.TotalDistanceDuringRide, elapsedTime));
        });

        await _summary.UpdateAsync(elapsedTime, _sharedDto.TotalDistanceDuringRide);
        _sharedDto.TotalDistanceDuringRide = 0;
    }


    private void PauseButton_Clicked(object sender, EventArgs e)
    {
        // Mettre en pause le chronom�tre lorsque le bouton "Pause" est cliqu�
        timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        stopButton.IsVisible = false;
        pauseButton.IsVisible = false;
        startButton.IsVisible = false;
        resumeButton.IsVisible = true;

        isTimerRunning = false;
    }

    private void ResumeButton_Clicked(object sender, EventArgs e)
    {
        // Reprendre le chronom�tre lorsque le bouton "Resume" est cliqu�
        timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));

        stopButton.IsVisible = true;
        pauseButton.IsVisible = true;
        startButton.IsVisible = false;
        resumeButton.IsVisible = false;

        isTimerRunning = true;
    }
}