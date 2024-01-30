using Car_kilometer.NewFolder;

namespace Car_kilometer;

public partial class MapPage : ContentPage
{
    private Timer timer;
    private TimeSpan elapsedTime;
    private bool isTimerRunning = false;
    private Statistic statistic = new Statistic();

    public MapPage()
	{
		InitializeComponent();

        timer = new Timer(TimerCallback, null, Timeout.InfiniteTimeSpan, TimeSpan.FromSeconds(1));
    }

    private void TimerCallback(object state)
    {
        elapsedTime = elapsedTime.Add(TimeSpan.FromSeconds(1));

        Device.BeginInvokeOnMainThread(() =>
        {
            timerLabel.Text = elapsedTime.ToString(@"hh\:mm\:ss");
        });
    }

    private void StartButton_Clicked(object sender, EventArgs e)
    {
        if (!isTimerRunning)
        {
            // Démarrer le chronomètre lorsque le bouton "Start" est cliqué
            timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));

            // Afficher les boutons Stop et Pause, et masquer le bouton Start
            stopButton.IsVisible = true;
            pauseButton.IsVisible = true;
            startButton.IsVisible = false;

            // Inverser l'état
            isTimerRunning = true;
        }
    }

    private void StopButton_Clicked(object sender, EventArgs e)
    {
        if (isTimerRunning)
        {
            // Arrêter le chronomètre lorsque le bouton "Stop" est cliqué
            timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            // Ajouter le trajet à la liste des trajets avec le temps actuel du chronomètre
            Ride newRide = new Ride(distance: 0, duration: elapsedTime);
            statistic.rides.Add(newRide);

            statistic.TotalDistance += newRide.Distance;
            statistic.TotalDuration += newRide.Duration;

            // Masquer les boutons Stop et Pause, et afficher le bouton Start
            stopButton.IsVisible = false;
            pauseButton.IsVisible = false;
            startButton.IsVisible = true;

            // Réinitialiser le chronomètre et l'interface utilisateur
            elapsedTime = TimeSpan.Zero;
            Device.BeginInvokeOnMainThread(() =>
            {
                timerLabel.Text = elapsedTime.ToString(@"hh\:mm\:ss");
            });

            // Inverser l'état
            isTimerRunning = false;
        }
    }

    private void PauseButton_Clicked(object sender, EventArgs e)
    {
        // Mettre en pause le chronomètre lorsque le bouton "Pause" est cliqué
        timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        // Masquer les boutons Stop et Pause, et afficher le bouton Start
        stopButton.IsVisible = false;
        pauseButton.IsVisible = false;
        startButton.IsVisible = true;

        // Inverser l'état
        isTimerRunning = false;
    }
}