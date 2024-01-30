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
            // D�marrer le chronom�tre lorsque le bouton "Start" est cliqu�
            timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));

            // Afficher les boutons Stop et Pause, et masquer le bouton Start
            stopButton.IsVisible = true;
            pauseButton.IsVisible = true;
            startButton.IsVisible = false;

            // Inverser l'�tat
            isTimerRunning = true;
        }
    }

    private void StopButton_Clicked(object sender, EventArgs e)
    {
        if (isTimerRunning)
        {
            // Arr�ter le chronom�tre lorsque le bouton "Stop" est cliqu�
            timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            // Ajouter le trajet � la liste des trajets avec le temps actuel du chronom�tre
            Ride newRide = new Ride(distance: 0, duration: elapsedTime);
            statistic.rides.Add(newRide);

            statistic.TotalDistance += newRide.Distance;
            statistic.TotalDuration += newRide.Duration;

            // Masquer les boutons Stop et Pause, et afficher le bouton Start
            stopButton.IsVisible = false;
            pauseButton.IsVisible = false;
            startButton.IsVisible = true;

            // R�initialiser le chronom�tre et l'interface utilisateur
            elapsedTime = TimeSpan.Zero;
            Device.BeginInvokeOnMainThread(() =>
            {
                timerLabel.Text = elapsedTime.ToString(@"hh\:mm\:ss");
            });

            // Inverser l'�tat
            isTimerRunning = false;
        }
    }

    private void PauseButton_Clicked(object sender, EventArgs e)
    {
        // Mettre en pause le chronom�tre lorsque le bouton "Pause" est cliqu�
        timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        // Masquer les boutons Stop et Pause, et afficher le bouton Start
        stopButton.IsVisible = false;
        pauseButton.IsVisible = false;
        startButton.IsVisible = true;

        // Inverser l'�tat
        isTimerRunning = false;
    }
}