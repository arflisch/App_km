using Car_kilometer.ViewModels;

namespace Car_kilometer.Views;

public partial class ExportPage : ContentPage
{
    readonly ExportViewModel _viewModel;

    public ExportPage(ExportViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }

    void OnPeriodTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: PeriodOption option })
            _viewModel.SelectPeriod(option);
    }

    void OnDocumentTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: DocumentOption option })
            _viewModel.SelectDocument(option);
    }
}
