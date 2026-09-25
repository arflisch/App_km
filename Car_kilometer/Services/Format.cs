using System.Globalization;

namespace Car_kilometer.Services;

/// <summary>Formats distances, durations and dates in the phone's language.</summary>
public static class Format
{
    static CultureInfo Culture => CultureInfo.CurrentCulture;

    public static string Number(double value, int decimals = 1) => value.ToString("N" + decimals, Culture);

    public static string Km(double km) => $"{Number(km)} km";

    public static string Speed(double kmh) => $"{Number(kmh, 0)} km/h";

    /// <summary>"42 min" or "1 h 05".</summary>
    public static string Duration(TimeSpan duration) => duration.TotalHours >= 1
        ? string.Format(Culture, AppResources.DurationHoursFormat, (int)duration.TotalHours, duration.Minutes)
        : string.Format(Culture, AppResources.DurationMinutesFormat, Math.Max(0, (int)duration.TotalMinutes));

    /// <summary>Stopwatch style "01:02:03".</summary>
    public static string Clock(TimeSpan duration) => $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";

    /// <summary>"jeu. 25 sept." / "Thu 25 Sep"</summary>
    public static string ShortDate(DateTime date) => date.ToString("ddd d MMM", Culture);

    public static string Date(DateTime date) => date.ToString("d", Culture);

    public static string Time(DateTime date) => date.ToString("t", Culture);

    /// <summary>"Septembre 2026"</summary>
    public static string Month(DateTime date) => Capitalize(date.ToString("MMMM yyyy", Culture));

    /// <summary>"sept." / "Sep"</summary>
    public static string MonthShort(DateTime date) => Capitalize(date.ToString("MMM", Culture).TrimEnd('.'));

    /// <summary>"Jeudi 25 septembre"</summary>
    public static string Today(DateTime date) =>
        Capitalize($"{date.ToString("dddd", Culture)} {date.ToString(Culture.DateTimeFormat.MonthDayPattern, Culture)}");

    public static string RideCount(int count) =>
        string.Format(Culture, count == 1 ? AppResources.RideCountOne : AppResources.RideCountMany, count);

    public static string Capitalize(string text) =>
        string.IsNullOrEmpty(text) ? text : char.ToUpper(text[0], Culture) + text[1..];
}
