namespace IForm.Web.Services;

/// <summary>
/// Working-hours rules for a manual attendance record. Hours are derived from
/// the check-in and check-out pair so a negative or oversized day cannot be stored.
/// </summary>
public static class AttendanceCalculator
{
    /// <summary>A shift longer than this is treated as a data-entry error.</summary>
    public const decimal MaxHoursPerDay = 16m;

    /// <summary>
    /// Hours between check-in and check-out, rounded to two places.
    /// Returns 0 when either end of the day is missing.
    /// </summary>
    public static decimal HoursWorked(DateTime? checkIn, DateTime? checkOut)
    {
        if (checkIn is null || checkOut is null)
        {
            return 0m;
        }

        if (checkOut <= checkIn)
        {
            return 0m;
        }

        return Math.Round((decimal)(checkOut.Value - checkIn.Value).TotalHours, 2);
    }

    /// <summary>
    /// Full day needs at least this many hours, a half day at least <see cref="HalfDayMinimumHours"/>.
    /// </summary>
    public const decimal FullDayMinimumHours = 6m;

    public const decimal HalfDayMinimumHours = 3m;

    /// <summary>
    /// Derives the day's status from hours worked. Returns null when the hours are
    /// implausible, so the caller can reject the input instead of inventing a status.
    /// </summary>
    public static Models.AttendanceStatus? StatusFor(decimal hoursWorked)
    {
        if (hoursWorked < 0m || hoursWorked > MaxHoursPerDay)
        {
            return null;
        }

        if (hoursWorked == 0m)
        {
            return Models.AttendanceStatus.Absent;
        }

        if (hoursWorked >= FullDayMinimumHours)
        {
            return Models.AttendanceStatus.Present;
        }

        if (hoursWorked >= HalfDayMinimumHours)
        {
            return Models.AttendanceStatus.HalfDay;
        }

        return Models.AttendanceStatus.HalfDay;
    }

    /// <summary>
    /// Checks the pair of timestamps. Returns null when valid, otherwise the reason.
    /// </summary>
    public static string? Validate(DateTime? checkIn, DateTime? checkOut)
    {
        if (checkIn is null && checkOut is null)
        {
            return null;
        }

        if (checkIn is null || checkOut is null)
        {
            return "Provide both check-in and check-out, or neither.";
        }

        if (checkOut <= checkIn)
        {
            return "Check-out must be later than check-in.";
        }

        var hours = HoursWorked(checkIn, checkOut);
        if (hours > MaxHoursPerDay)
        {
            return $"That is {hours:0.##} hours, more than the {MaxHoursPerDay:0} hour daily maximum.";
        }

        return null;
    }
}