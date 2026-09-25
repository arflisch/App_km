namespace Car_kilometer.Services;

/// <summary>Alerts and confirmations shown from the view models, on top of the visible page.</summary>
public sealed class DialogService
{
    static Page CurrentPage
    {
        get
        {
            var page = Application.Current!.Windows[0].Page!;
            // A modal page (ride editor, export) sits on top of the shell
            return page.Navigation.ModalStack.LastOrDefault() ?? page;
        }
    }

    public Task AlertAsync(string title, string message) =>
        CurrentPage.DisplayAlertAsync(title, message, AppResources.Ok);

    public Task<bool> ConfirmAsync(string title, string message, string accept, string? cancel = null) =>
        CurrentPage.DisplayAlertAsync(title, message, accept, cancel ?? AppResources.Cancel);
}
