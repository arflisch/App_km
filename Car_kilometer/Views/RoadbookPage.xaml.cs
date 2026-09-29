using Car_kilometer.ViewModels;

namespace Car_kilometer.Views;

public partial class RoadbookPage : ContentPage
{
    public RoadbookPage(RoadbookViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
