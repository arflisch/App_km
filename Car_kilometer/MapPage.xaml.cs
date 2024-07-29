using AndroidX.Browser.Trusted.Sharing;
using Car_kilometer.Models;
using Car_kilometer.NewFolder;
using Car_kilometer.Services;
using Shiny.Locations;

namespace Car_kilometer;

public partial class MapPage : ContentPage
{
    private Timer? timer;
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
            // Démarrer le trajet
            rideStartTime = DateTime.Now;
            isTimerRunning = true;

            // Démarrer le timer pour mettre à jour l'interface utilisateur
            timer = new Timer(UpdateUI, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

            // Afficher les boutons appropriés
            stopButton.IsVisible = true;
            pauseButton.IsVisible = true;
            startButton.IsVisible = false;

            // Arrêter l'écouteur GPS actuel
            if (_gpsManager.CurrentListener != null)
            {
                await _gpsManager.StopListener();
            }

            // Démarrer un nouvel écouteur GPS
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
    private void UpdateUI(object? state)
    {
        Dispatcher.Dispatch(() =>
        {
            // Calculer le temps écoulé depuis le début du trajet
            elapsedTime = DateTime.Now - rideStartTime;

            // Lecture des valeurs de _sharedDto
            double totalDistance;
            double speed;

            lock (_lock)
            {
                totalDistance = _sharedDto.TotalDistanceDuringRide;
                speed = _sharedDto.Speed;
            }

            // Conversion des unités et mise à jour de l'interface utilisateur
            double totalDistanceKm = (totalDistance / 1000);
            double speedKmH = (speed * 3.6);

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
                // L'utilisateur a choisi "Oui", donc arrêter le chronomètre et réinitialiser
                timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

                stopButton.IsVisible = false;
                pauseButton.IsVisible = false;
                resumeButton.IsVisible = false;
                startButton.IsVisible = true;

                await Add();

                // Réinitialiser le chronomètre et l'interface utilisateur
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
                // L'utilisateur a choisi "Non", donc reprendre le chronomètre
                timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            }
        }
    }

    private async Task Add()
    {
        //Statistic statistic = await _summary.GetStatisticAsync();
        double _distance = _sharedDto.TotalDistanceDuringRide / 1000;
        await _summary.UpdateAsync(elapsedTime, _distance, new Ride(_distance, elapsedTime));
        _sharedDto.TotalDistanceDuringRide = 0;
    }


    private void PauseButton_Clicked(object sender, EventArgs e)
    {
        // Mettre en pause le chronomètre lorsque le bouton "Pause" est cliqué
        if (timer != null)
        {
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
        // Reprendre le chronomètre lorsque le bouton "Resume" est cliqué
        if (timer != null)
        {
            timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));

            stopButton.IsVisible = true;
            pauseButton.IsVisible = true;
            startButton.IsVisible = false;
            resumeButton.IsVisible = false;

            isTimerRunning = true;
        }
    }
}