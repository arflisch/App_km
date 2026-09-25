namespace Car_kilometer.Services;

/// <summary>What the user typed once and should not have to type again.</summary>
public sealed class UserSettings
{
    public string FirstName
    {
        get => Preferences.Default.Get("driver.firstName", string.Empty);
        set => Preferences.Default.Set("driver.firstName", value.Trim());
    }

    public string LastName
    {
        get => Preferences.Default.Get("driver.lastName", string.Empty);
        set => Preferences.Default.Set("driver.lastName", value.Trim());
    }

    public string RecipientEmail
    {
        get => Preferences.Default.Get("report.recipient", string.Empty);
        set => Preferences.Default.Set("report.recipient", value.Trim());
    }
}
