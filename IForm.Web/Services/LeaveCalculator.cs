namespace IForm.Web.Services;

/// <summary>
/// Leave balance and day-count rules. Kept separate from the controller so the
/// arithmetic can be tested directly and so every caller counts days the same way.
/// </summary>
public static class LeaveCalculator
{
    /// <summary>
    /// Counts leave days between two dates inclusive, skipping weekends and any
    /// date present in <paramref name="holidays"/>. A range that contains no
    /// working days returns 0 rather than 1.
    /// </summary>
    public static decimal CountWorkingDays(DateTime start, DateTime end, ISet<DateTime>? holidays = null)
    {
        if (end.Date < start.Date)
        {
            return 0m;
        }

        var days = 0m;
        for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
        {
            if (IsWeekend(d))
            {
                continue;
            }

            if (holidays is not null && holidays.Contains(d))
            {
                continue;
            }

            days += 1m;
        }

        return days;
    }

    public static bool IsWeekend(DateTime date) =>
        date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    /// <summary>
    /// Balance for an employee and leave type as the sum of their ledger.
    /// Returns 0 when they have no ledger entries rather than guessing an allowance.
    /// </summary>
    public static decimal BalanceFromLedger(IEnumerable<(Models.LeaveType LeaveType, decimal Days)> ledger, Models.LeaveType type) =>
        ledger.Where(l => l.LeaveType == type).Sum(l => l.Days);

    /// <summary>
    /// Validates that a request can be approved against the current balance.
    /// Returns null when it is fine, otherwise the reason it is not.
    /// </summary>
    public static string? ValidateApproval(decimal currentBalance, decimal requestedDays, bool overlap)
    {
        if (overlap)
        {
            return "This request overlaps leave the employee already has.";
        }

        if (requestedDays > currentBalance)
        {
            return $"Request exceeds the available balance. Balance is {currentBalance:0.##} day(s), request is {requestedDays:0.##}.";
        }

        return null;
    }
}