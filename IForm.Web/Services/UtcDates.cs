namespace IForm.Web.Services;

/// <summary>
/// Npgsql refuses to write a <see cref="DateTime"/> whose <see cref="DateTime.Kind"/> is not
/// <see cref="DateTimeKind.Utc"/> to a timestamptz column, while SQLite accepts anything.
/// Values that arrive from an HTML form are <see cref="DateTimeKind.Unspecified"/>, so HR
/// writes have to be stamped before they reach the database.
/// </summary>
public static class UtcDates
{
    /// <summary>
    /// Stamps a calendar date (no meaningful time of day) as UTC midnight, keeping the
    /// date the user typed regardless of the server's own time zone.
    /// </summary>
    public static DateTime Date(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    /// <summary>Stamps a nullable calendar date, passing null through.</summary>
    public static DateTime? Date(DateTime? value) =>
        value.HasValue ? Date(value.Value) : null;

    /// <summary>
    /// Converts a wall-clock time the user entered (for example a check-in time) from the
    /// server's local zone to UTC, matching how the rest of the app stores instants.
    /// </summary>
    public static DateTime Instant(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime();

    /// <summary>Converts a nullable wall-clock instant, passing null through.</summary>
    public static DateTime? Instant(DateTime? value) =>
        value.HasValue ? Instant(value.Value) : null;
}