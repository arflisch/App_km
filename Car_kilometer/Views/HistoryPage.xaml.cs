using Car_kilometer.ViewModels;

namespace Car_kilometer.Views;

public partial class HistoryPage : ContentPage
{
    readonly HistoryViewModel _viewModel;

    public HistoryPage(HistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }

    async void OnRideTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: RideItem ride })
            await _viewModel.OpenRideAsync(ride);
    }

    async void OnDeleteInvoked(object? sender, EventArgs e)
    {
        if (sender is BindableObject { BindingContext: RideItem ride })
            await _viewModel.DeleteAsync(ride);
    }
}
