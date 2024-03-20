using CommunityToolkit.Mvvm.ComponentModel;

namespace Car_kilometer.ViewModels;

public partial class BaseViewModel: ObservableObject
{
    public BaseViewModel(string title)
    {
        Title = title;
    }

    [ObservableProperty]
    private string title;


}
