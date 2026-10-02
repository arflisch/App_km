namespace Car_kilometer.Services;

/// <summary>What the user typed once and should not have to type again.</summary>
public sealed class UserSettings
{
    public const int DefaultRoadbookGoalKm = 1500;

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

    // ---- Roadbook (learner drivers practising with a guide)

    public bool RoadbookEnabled
    {
        get => Preferences.Default.Get("roadbook.enabled", false);
        set => Preferences.Default.Set("roadbook.enabled", value);
    }

    /// <summary>Only the rides from this day on count for the roadbook.</summary>
    public DateTime RoadbookStart
    {
        get => Preferences.Default.Get("roadbook.start", DateTime.Today);
        set => Preferences.Default.Set("roadbook.start", value.Date);
    }

    public int RoadbookGoalKm
    {
        get => Preferences.Default.Get("roadbook.goalKm", DefaultRoadbookGoalKm);
        set => Preferences.Default.Set("roadbook.goalKm", value);
    }

    public string Guide1
    {
        get => Preferences.Default.Get("roadbook.guide1", string.Empty);
        set => Preferences.Default.Set("roadbook.guide1", value.Trim());
    }

    public string Guide2
    {
        get => Preferences.Default.Get("roadbook.guide2", string.Empty);
        set => Preferences.Default.Set("roadbook.guide2", value.Trim());
    }

    /// <summary>The guide of the last saved ride, preselected for the next one.</summary>
    public string LastGuide
    {
        get => Preferences.Default.Get("roadbook.lastGuide", string.Empty);
        set => Preferences.Default.Set("roadbook.lastGuide", value);
    }

    public IReadOnlyList<string> Guides =>
        new[] { Guide1, Guide2 }.Where(guide => guide.Length > 0).Distinct(StringComparer.CurrentCultureIgnoreCase).ToArray();
}
