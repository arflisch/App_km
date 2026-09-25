using Car_kilometer.ViewModels;

namespace Car_kilometer.Views;

public partial class RideEditorPage : ContentPage
{
    readonly RideEditorViewModel _viewModel;

    public RideEditorPage(RideEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    void OnWeatherTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: WeatherOption option })
            _viewModel.SelectWeather(option);
    }
}
