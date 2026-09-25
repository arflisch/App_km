using System.Globalization;
using System.Net.Mail;
using Microsoft.Extensions.Logging;

namespace Car_kilometer.ViewModels;

public enum ExportPeriod { ThisMonth, LastMonth, ThisYear, All, Custom }

public sealed partial class PeriodOption(ExportPeriod period, string label) : ObservableObject
{
    public ExportPeriod Period { get; } = period;
    public string Label { get; } = label;

    [ObservableProperty] public partial bool IsSelected { get; set; }
}

public sealed partial class ExportViewModel : ObservableObject
{
    readonly RideRepository _repository;
    readonly PdfReportService _pdf;
    readonly UserSettings _settings;
    readonly DialogService _dialogs;
    readonly ILogger<ExportViewModel> _logger;
    IReadOnlyList<RideItem> _rides = [];

    public ExportViewModel(RideRepository repository, PdfReportService pdf, UserSettings settings, DialogService dialogs, ILogger<ExportViewModel> logger)
    {
        _repository = repository;
        _pdf = pdf;
        _settings = settings;
        _dialogs = dialogs;
        _logger = logger;

        FirstName = settings.FirstName;
        LastName = settings.LastName;
        RecipientEmail = settings.RecipientEmail;
        var today = DateTime.Today;
        CustomFrom = new DateTime(today.Year, today.Month, 1);
        CustomTo = today;
        OnSelectedPeriodChanged(SelectedPeriod);
    }

    public IReadOnlyList<PeriodOption> Periods { get; } =
    [
        new(ExportPeriod.ThisMonth, AppResources.PeriodThisMonth),
        new(ExportPeriod.LastMonth, AppResources.PeriodLastMonth),
        new(ExportPeriod.ThisYear, AppResources.PeriodThisYear),
        new(ExportPeriod.All, AppResources.PeriodAll),
        new(ExportPeriod.Custom, AppResources.PeriodCustom),
    ];

    [ObservableProperty] public partial ExportPeriod SelectedPeriod { get; set; }
    [ObservableProperty] public partial bool IsCustom { get; set; }
    [ObservableProperty] public partial DateTime CustomFrom { get; set; }
    [ObservableProperty] public partial DateTime CustomTo { get; set; }
    [ObservableProperty] public partial string FirstName { get; set; } = string.Empty;
    [ObservableProperty] public partial string LastName { get; set; } = string.Empty;
    [ObservableProperty] public partial string RecipientEmail { get; set; } = string.Empty;
    [ObservableProperty] public partial string PreviewTitle { get; set; } = string.Empty;
    [ObservableProperty] public partial string PreviewPeriod { get; set; } = string.Empty;
    [ObservableProperty] public partial bool HasRides { get; set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? Error { get; set; }

    public bool HasError => Error is not null;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendEmailCommand), nameof(SharePdfCommand))]
    public partial bool IsBusy { get; set; }

    public async Task OnAppearingAsync()
    {
        _rides = await _repository.GetRidesAsync();
        UpdatePreview();
    }

    public void SelectPeriod(PeriodOption option) => SelectedPeriod = option.Period;

    partial void OnSelectedPeriodChanged(ExportPeriod value)
    {
        foreach (var option in Periods)
            option.IsSelected = option.Period == value;
        IsCustom = value == ExportPeriod.Custom;
        UpdatePreview();
    }

    partial void OnCustomFromChanged(DateTime value) => UpdatePreview();

    partial void OnCustomToChanged(DateTime value) => UpdatePreview();

    [RelayCommand]
    Task Close() => Shell.Current.GoToAsync("..");

    [RelayCommand(CanExecute = nameof(CanExport))]
    async Task SendEmail()
    {
        if (!Validate(requireEmail: true) || await CreatePdfAsync() is not { } file)
            return;

        if (!Email.Default.IsComposeSupported)
        {
            await _dialogs.AlertAsync(AppResources.EmailUnavailableTitle, AppResources.EmailUnavailableMessage);
            await ShareFileAsync(file);
            return;
        }

        var (from, to) = GetRange();
        var culture = CultureInfo.CurrentCulture;
        var message = new EmailMessage
        {
            Subject = string.Format(culture, AppResources.EmailSubject, Format.Date(from), Format.Date(to)),
            Body = string.Format(culture, AppResources.EmailBody, Format.Date(from), Format.Date(to), PreviewTitle, $"{FirstName.Trim()} {LastName.Trim()}"),
            BodyFormat = EmailBodyFormat.PlainText,
            To = [RecipientEmail.Trim()],
            Attachments = [new EmailAttachment(file)],
        };

        try
        {
            await Email.Default.ComposeAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not open the email composer");
            await ShareFileAsync(file);
        }
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    async Task SharePdf()
    {
        if (Validate(requireEmail: false) && await CreatePdfAsync() is { } file)
            await ShareFileAsync(file);
    }

    bool CanExport() => !IsBusy;

    bool Validate(bool requireEmail)
    {
        Error = null;
        if (!HasRides)
            Error = AppResources.NoRidesInPeriod;
        else if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
            Error = AppResources.NameRequired;
        else if (requireEmail && !IsValidEmail(RecipientEmail))
            Error = AppResources.EmailInvalid;

        if (Error is not null)
            return false;

        _settings.FirstName = FirstName;
        _settings.LastName = LastName;
        if (!string.IsNullOrWhiteSpace(RecipientEmail))
            _settings.RecipientEmail = RecipientEmail;
        return true;
    }

    async Task<string?> CreatePdfAsync()
    {
        IsBusy = true;
        try
        {
            var (from, to) = GetRange();
            var file = Path.Combine(FileSystem.CacheDirectory, $"{AppResources.PdfFileName}_{from:yyyy-MM-dd}_{to:yyyy-MM-dd}.pdf");
            await _pdf.CreateAsync(new ReportRequest(RidesIn(from, to), from, to, FirstName.Trim(), LastName.Trim()), file);
            return file;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF generation failed");
            await _dialogs.AlertAsync(AppResources.ErrorTitle, AppResources.PdfError);
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    static Task ShareFileAsync(string file) => Share.Default.RequestAsync(new ShareFileRequest
    {
        Title = AppResources.PdfTitle,
        File = new ShareFile(file, "application/pdf"),
    });

    (DateTime From, DateTime To) GetRange()
    {
        var today = DateTime.Today;
        var month = new DateTime(today.Year, today.Month, 1);
        return SelectedPeriod switch
        {
            ExportPeriod.ThisMonth => (month, month.AddMonths(1).AddDays(-1)),
            ExportPeriod.LastMonth => (month.AddMonths(-1), month.AddDays(-1)),
            ExportPeriod.ThisYear => (new DateTime(today.Year, 1, 1), new DateTime(today.Year, 12, 31)),
            ExportPeriod.All when _rides.Count > 0 => (_rides.Min(r => r.LocalDate).Date, _rides.Max(r => r.LocalDate).Date),
            ExportPeriod.All => (today, today),
            _ => CustomFrom <= CustomTo ? (CustomFrom.Date, CustomTo.Date) : (CustomTo.Date, CustomFrom.Date),
        };
    }

    List<RideItem> RidesIn(DateTime from, DateTime to) =>
        _rides.Where(r => r.LocalDate.Date >= from && r.LocalDate.Date <= to).ToList();

    void UpdatePreview()
    {
        var (from, to) = GetRange();
        var rides = RidesIn(from, to);
        HasRides = rides.Count > 0;
        PreviewTitle = HasRides
            ? $"{Format.RideCount(rides.Count)} · {Format.Km(rides.Sum(r => r.DistanceKm))}"
            : AppResources.NoRidesInPeriod;
        PreviewPeriod = string.Format(CultureInfo.CurrentCulture, AppResources.PeriodRange, Format.Date(from), Format.Date(to));
        if (HasRides && Error == AppResources.NoRidesInPeriod)
            Error = null;
    }

    static bool IsValidEmail(string? value) =>
        MailAddress.TryCreate(value?.Trim(), out var address) && address.Host.Contains('.');
}
