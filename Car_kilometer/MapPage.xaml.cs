using Car_kilometer.NewFolder;
using Car_kilometer.Services;

namespace Car_kilometer;

public partial class MapPage : ContentPage
{
    private Timer timer;
    private TimeSpan elapsedTime;
    private bool isTimerRunning = false;

    public MapPage()
    {
        InitializeComponent();
        _summary = MauiProgram.ServiceProvider.GetRequiredService<Summary>();
        timer = new Timer(TimerCallback, null, Timeout.InfiniteTimeSpan, TimeSpan.FromSeconds(1));
    }

    private Summary _summary;

    private async void TimerCallback(object state)
    { 
        await _summary.GetStatisticAsync();

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

            stopButton.IsVisible = true;
            pauseButton.IsVisible = true;
            startButton.IsVisible = false;

            isTimerRunning = true;
        }
    }

    private async void StopButton_Clicked(object sender, EventArgs e)
    {
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
            Device.BeginInvokeOnMainThread(() =>
            {
                timerLabel.Text = elapsedTime.ToString(@"hh\:mm\:ss");
            });

            isTimerRunning = false;
        }
        else
        {
            // L'utilisateur a choisi "Non", donc reprendre le chronomètre
            timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }
    }

    private async Task Add()
    {
        var realm = await Realm.GetInstanceAsync();

        await realm.WriteAsync(() =>
        {
            realm.Add(new Ride(0, elapsedTime));
        });

        await _summary.UpdateAsync(elapsedTime);
    }

    private void PauseButton_Clicked(object sender, EventArgs e)
    {
        // Mettre en pause le chronomètre lorsque le bouton "Pause" est cliqué
        timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        stopButton.IsVisible = false;
        pauseButton.IsVisible = false;
        startButton.IsVisible = false;
        resumeButton.IsVisible = true;

        isTimerRunning = false;
    }

    private void ResumeButton_Clicked(object sender, EventArgs e)
    {
        // Reprendre le chronomètre lorsque le bouton "Resume" est cliqué
        timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));

        stopButton.IsVisible = true;
        pauseButton.IsVisible = true;
        startButton.IsVisible = false;
        resumeButton.IsVisible = false;

        isTimerRunning = true;
    }
}