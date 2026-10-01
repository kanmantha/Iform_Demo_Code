using IForm.Web.Models;
using IForm.Web.Services;

namespace IForm.Tests;

public class HrCalculatorTests
{
    // 2026-03-02 is a Monday and 2026-03-08 the Sunday of the same week.
    private static readonly DateTime Monday = new(2026, 3, 2);
    private static readonly DateTime Friday = new(2026, 3, 6);
    private static readonly DateTime Saturday = new(2026, 3, 7);
    private static readonly DateTime Sunday = new(2026, 3, 8);
    private static readonly DateTime NextMonday = new(2026, 3, 9);

    [Fact]
    public void CountWorkingDays_CountsWeekdaysInclusively()
    {
        Assert.Equal(5m, LeaveCalculator.CountWorkingDays(Monday, Friday));
    }

    [Fact]
    public void CountWorkingDays_SkipsWeekends()
    {
        // Friday to Monday spans a weekend, so only Friday and Monday count.
        Assert.Equal(2m, LeaveCalculator.CountWorkingDays(Friday, NextMonday));
    }

    [Fact]
    public void CountWorkingDays_SingleDayCountsAsOne()
    {
        Assert.Equal(1m, LeaveCalculator.CountWorkingDays(Monday, Monday));
    }

    [Fact]
    public void CountWorkingDays_WeekendOnlyRangeIsZero()
    {
        Assert.Equal(0m, LeaveCalculator.CountWorkingDays(Saturday, Sunday));
    }

    [Fact]
    public void CountWorkingDays_RejectsReversedRange()
    {
        Assert.Equal(0m, LeaveCalculator.CountWorkingDays(Friday, Monday));
    }

    [Fact]
    public void CountWorkingDays_ExcludesHolidays()
    {
        var holidays = new HashSet<DateTime> { new(2026, 3, 4) };

        Assert.Equal(4m, LeaveCalculator.CountWorkingDays(Monday, Friday, holidays));
    }

    [Fact]
    public void IsWeekend_RecognisesSaturdayAndSundayOnly()
    {
        Assert.True(LeaveCalculator.IsWeekend(Saturday));
        Assert.True(LeaveCalculator.IsWeekend(Sunday));
        Assert.False(LeaveCalculator.IsWeekend(Monday));
        Assert.False(LeaveCalculator.IsWeekend(Friday));
    }

    [Fact]
    public void BalanceFromLedger_SumsOnlyTheRequestedType()
    {
        var ledger = new (LeaveType LeaveType, decimal Days)[]
        {
            (LeaveType.Casual, 6m),
            (LeaveType.Sick, 4m),
            (LeaveType.Casual, -2m)
        };

        Assert.Equal(4m, LeaveCalculator.BalanceFromLedger(ledger, LeaveType.Casual));
        Assert.Equal(4m, LeaveCalculator.BalanceFromLedger(ledger, LeaveType.Sick));
        Assert.Equal(0m, LeaveCalculator.BalanceFromLedger(ledger, LeaveType.Earned));
    }

    [Fact]
    public void BalanceFromLedger_NoEntriesIsZero()
    {
        Assert.Equal(0m, LeaveCalculator.BalanceFromLedger(
            Array.Empty<(LeaveType, decimal)>(), LeaveType.Casual));
    }

    [Fact]
    public void ValidateApproval_AllowsRequestWithinBalance()
    {
        Assert.Null(LeaveCalculator.ValidateApproval(currentBalance: 10m, requestedDays: 4m, overlap: false));
    }

    [Fact]
    public void ValidateApproval_AllowsRequestEqualToBalance()
    {
        Assert.Null(LeaveCalculator.ValidateApproval(currentBalance: 5m, requestedDays: 5m, overlap: false));
    }

    [Fact]
    public void ValidateApproval_BlocksRequestExceedingBalance()
    {
        var problem = LeaveCalculator.ValidateApproval(currentBalance: 3m, requestedDays: 4m, overlap: false);

        Assert.NotNull(problem);
        Assert.Contains("exceeds the available balance", problem, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateApproval_BlocksOverlappingRequest()
    {
        var problem = LeaveCalculator.ValidateApproval(currentBalance: 30m, requestedDays: 1m, overlap: true);

        Assert.NotNull(problem);
        Assert.Contains("overlaps", problem, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateApproval_ZeroBalanceBlocksAnyPositiveRequest()
    {
        Assert.NotNull(LeaveCalculator.ValidateApproval(currentBalance: 0m, requestedDays: 1m, overlap: false));
    }

    [Fact]
    public void HoursWorked_IsZeroWhenEitherTimeIsMissing()
    {
        var morning = new DateTime(2026, 3, 2, 9, 0, 0);

        Assert.Equal(0m, AttendanceCalculator.HoursWorked(null, null));
        Assert.Equal(0m, AttendanceCalculator.HoursWorked(morning, null));
        Assert.Equal(0m, AttendanceCalculator.HoursWorked(null, morning));
    }

    [Fact]
    public void HoursWorked_IsZeroWhenCheckoutIsNotAfterCheckin()
    {
        var morning = new DateTime(2026, 3, 2, 9, 0, 0);

        Assert.Equal(0m, AttendanceCalculator.HoursWorked(morning, morning));
        Assert.Equal(0m, AttendanceCalculator.HoursWorked(morning, morning.AddHours(-2)));
    }

    [Fact]
    public void HoursWorked_DifferencesTheTwoTimes()
    {
        var checkIn = new DateTime(2026, 3, 2, 9, 0, 0);
        var checkOut = new DateTime(2026, 3, 2, 18, 30, 0);

        Assert.Equal(9.5m, AttendanceCalculator.HoursWorked(checkIn, checkOut));
    }

    [Fact]
    public void StatusFor_SixHoursOrMoreIsPresent()
    {
        Assert.Equal(AttendanceStatus.Present, AttendanceCalculator.StatusFor(6m));
        Assert.Equal(AttendanceStatus.Present, AttendanceCalculator.StatusFor(9m));
    }

    [Fact]
    public void StatusFor_BetweenThreeAndSixHoursIsHalfDay()
    {
        Assert.Equal(AttendanceStatus.HalfDay, AttendanceCalculator.StatusFor(3m));
        Assert.Equal(AttendanceStatus.HalfDay, AttendanceCalculator.StatusFor(5.99m));
    }

    [Fact]
    public void StatusFor_UnderThreeHoursIsHalfDay()
    {
        Assert.Equal(AttendanceStatus.HalfDay, AttendanceCalculator.StatusFor(2.5m));
    }

    [Fact]
    public void StatusFor_ZeroHoursIsAbsent()
    {
        Assert.Equal(AttendanceStatus.Absent, AttendanceCalculator.StatusFor(0m));
    }

    [Fact]
    public void StatusFor_NegativeHoursIsRejected()
    {
        Assert.Null(AttendanceCalculator.StatusFor(-1m));
    }

    [Fact]
    public void StatusFor_MoreThanDailyMaximumIsRejected()
    {
        Assert.Null(AttendanceCalculator.StatusFor(AttendanceCalculator.MaxHoursPerDay + 0.01m));
    }

    [Fact]
    public void StatusFor_ExactlyTheDailyMaximumIsAccepted()
    {
        Assert.Equal(AttendanceStatus.Present, AttendanceCalculator.StatusFor(AttendanceCalculator.MaxHoursPerDay));
    }

    [Fact]
    public void Validate_AcceptsBothTimesAbsent()
    {
        Assert.Null(AttendanceCalculator.Validate(null, null));
    }

    [Fact]
    public void Validate_RejectsOnlyOneSideProvided()
    {
        var morning = new DateTime(2026, 3, 2, 9, 0, 0);

        Assert.NotNull(AttendanceCalculator.Validate(morning, null));
        Assert.NotNull(AttendanceCalculator.Validate(null, morning));
    }

    [Fact]
    public void Validate_RejectsCheckoutBeforeCheckin()
    {
        var morning = new DateTime(2026, 3, 2, 9, 0, 0);

        Assert.NotNull(AttendanceCalculator.Validate(morning, morning.AddHours(-1)));
    }

    [Fact]
    public void Validate_RejectsDayLongerThanTheMaximum()
    {
        var morning = new DateTime(2026, 3, 2, 6, 0, 0);

        Assert.NotNull(AttendanceCalculator.Validate(morning, morning.AddHours(17)));
    }

    [Fact]
    public void Validate_AcceptsNormalWorkingDay()
    {
        var checkIn = new DateTime(2026, 3, 2, 9, 0, 0);
        var checkOut = new DateTime(2026, 3, 2, 18, 0, 0);

        Assert.Null(AttendanceCalculator.Validate(checkIn, checkOut));
    }
}