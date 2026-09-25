using System.ComponentModel;
using Car_kilometer.ViewModels;

namespace Car_kilometer.Views;

public partial class RidePage : ContentPage
{
    const string PulseAnimation = "pulse";

    readonly RideViewModel _viewModel;
    readonly IDispatcherTimer _timer;
    bool _isShown;

    public RidePage(RideViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        // The tracker is read twice a second, and only while this page is on screen during a ride
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(500);
        _timer.Tick += (_, _) => _viewModel.Refresh();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isShown = true;
        _viewModel.Refresh();
        UpdateForState();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isShown = false;
        UpdateForState();
    }

    void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RideViewModel.State))
            UpdateForState();
    }

    void UpdateForState()
    {
        var recording = _isShown && _viewModel.State == TrackingState.Recording;

        if (recording)
            _timer.Start();
        else
            _timer.Stop();

        // Keeps the screen on while driving, so the speed stays readable in a phone holder
        DeviceDisplay.Current.KeepScreenOn = recording;

        this.AbortAnimation(PulseAnimation);
        StatusDot.Opacity = 1;
        if (recording)
        {
            new Animation
            {
                { 0, 0.5, new Animation(v => StatusDot.Opacity = v, 1, 0.2) },
                { 0.5, 1, new Animation(v => StatusDot.Opacity = v, 0.2, 1) },
            }.Commit(this, PulseAnimation, length: 1400, repeat: () => _isShown && _viewModel.State == TrackingState.Recording);
        }
    }
}
