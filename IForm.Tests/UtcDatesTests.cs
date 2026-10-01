using IForm.Web.Models;
using IForm.Web.Services;
using Xunit;

namespace IForm.Tests;

/// <summary>
/// Regression guard for a production-only failure: Npgsql refuses to write a DateTime whose
/// Kind is not Utc to a timestamptz column, so every value coming from an HTML form had to be
/// stamped before saving. SQLite accepted the Unspecified values, which is why the existing
/// controller tests all passed while the deployed app returned 500 on every create.
/// </summary>
public class UtcDatesTests
{
    [Fact]
    public void Date_StampsUnspecifiedFormValueAsUtcMidnight()
    {
        // What model binding produces for <input type="date" value="2026-10-01">.
        var fromForm = new DateTime(2026, 10, 1);

        var stamped = UtcDates.Date(fromForm);

        Assert.Equal(DateTimeKind.Utc, stamped.Kind);
        Assert.Equal(new DateTime(2026, 10, 1), stamped);
        Assert.Equal(TimeSpan.Zero, stamped.TimeOfDay);
    }

    [Fact]
    public void Date_DoesNotShiftTheCalendarDayForNonUtcServer()
    {
        // A date has no time component, so converting it as an instant would move it a day
        // backwards in any server east of UTC.
        var typed = new DateTime(2026, 1, 1);

        var stamped = UtcDates.Date(typed);

        Assert.Equal(1, stamped.Day);
        Assert.Equal(DateTimeKind.Utc, stamped.Kind);
    }

    [Fact]
    public void Date_PreservesTheTimeAlreadyCarriedByAUtcValue()
    {
        var utc = new DateTime(2026, 5, 3, 22, 15, 0, DateTimeKind.Utc);

        var stamped = UtcDates.Date(utc);

        Assert.Equal(DateTimeKind.Utc, stamped.Kind);
        Assert.Equal(new DateTime(2026, 5, 3), stamped);
    }

    [Fact]
    public void Date_NullablePassesNullThrough()
    {
        Assert.Null(UtcDates.Date((DateTime?)null));
        Assert.Equal(DateTimeKind.Utc, UtcDates.Date(new DateTime(2026, 3, 3)).Kind);
    }

    [Fact]
    public void Instant_ConvertsLocalWallClockToUtc()
    {
        var fromForm = new DateTime(2026, 10, 1, 9, 0, 0);

        var converted = UtcDates.Instant(fromForm);

        Assert.Equal(DateTimeKind.Utc, converted.Kind);
        Assert.Equal(fromForm, converted.ToLocalTime());
    }

    [Fact]
    public void Instant_NullablePassesNullThrough()
    {
        Assert.Null(UtcDates.Instant((DateTime?)null));
    }

    [Fact]
    public void EveryStampedValueIsWritableToPostgres()
    {
        // Npgsql only writes values that are Utc, which is exactly the rule these helpers
        // exist to satisfy.
        DateTime[] formBound =
        [
            UtcDates.Date(new DateTime(2026, 10, 1)),
            UtcDates.Instant(new DateTime(2026, 10, 1, 9, 0, 0)),
            UtcDates.Instant(new DateTime(2026, 10, 1, 18, 30, 0))
        ];

        Assert.All(formBound, value => Assert.Equal(DateTimeKind.Utc, value.Kind));
    }

    [Fact]
    public void AttendanceHoursAreUnaffectedByUtcStamping()
    {
        // Converting both ends by the same offset must not change the derived duration.
        var checkIn = new DateTime(2026, 10, 1, 9, 0, 0);
        var checkOut = new DateTime(2026, 10, 1, 18, 30, 0);

        var before = AttendanceCalculator.HoursWorked(checkIn, checkOut);
        var after = AttendanceCalculator.HoursWorked(
            UtcDates.Instant(checkIn).ToLocalTime(),
            UtcDates.Instant(checkOut).ToLocalTime());

        Assert.Equal(before, after);
        Assert.Equal(9.5m, after);
    }

    [Fact]
    public void LeaveWorkingDayCountIsUnaffectedByUtcStamping()
    {
        // Counting must depend on the calendar date, not on the time zone it is stored in.
        var start = new DateTime(2026, 10, 5);
        var end = new DateTime(2026, 10, 9);

        Assert.Equal(
            LeaveCalculator.CountWorkingDays(start, end),
            LeaveCalculator.CountWorkingDays(UtcDates.Date(start), UtcDates.Date(end)));
    }

    [Fact]
    public void AttendanceStatusIsDerivedFromHoursNotFromTimeZone()
    {
        // The controller derives hours from the wall-clock pair the user typed, then stores the
// stamped UTC pair. Both must agree.
        var typedCheckIn = new DateTime(2026, 10, 1, 9, 0, 0);
        var typedCheckOut = new DateTime(2026, 10, 1, 18, 30, 0);

        var hours = AttendanceCalculator.HoursWorked(typedCheckIn, typedCheckOut);

        var record = new AttendanceRecord
        {
            Date = UtcDates.Date(new DateTime(2026, 10, 1)),
            CheckIn = UtcDates.Instant(typedCheckIn),
            CheckOut = UtcDates.Instant(typedCheckOut),
            HoursWorked = hours,
            Status = AttendanceCalculator.StatusFor(hours)!.Value
        };

        Assert.Equal(9.5m, record.HoursWorked);
        Assert.Equal(AttendanceStatus.Present, record.Status);

        // Reading back the stored UTC pair must reproduce the same duration.
        Assert.Equal(
            hours,
            AttendanceCalculator.HoursWorked(record.CheckIn, record.CheckOut!.Value));
    }
}