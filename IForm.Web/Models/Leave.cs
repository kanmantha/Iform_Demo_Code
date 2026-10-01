using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

/// <summary>
/// A row in an employee's leave ledger. Every change to a balance is recorded here
/// so the balance is always the sum of the ledger rather than a number that can
/// drift out of step with the requests behind it.
/// </summary>
public class LeaveLedgerEntry
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public LeaveType LeaveType { get; set; }

    /// <summary>Positive grants an entitlement, negative consumes it.</summary>
    public decimal Days { get; set; }

    [MaxLength(200)]
    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? LeaveRequestId { get; set; }

    public LeaveRequest? LeaveRequest { get; set; }
}

public class LeaveRequest
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string RequestNumber { get; set; } = string.Empty;

    public int EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public LeaveType LeaveType { get; set; }

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Inclusive day count, weekends and holidays excluded. Computed by the
    /// controller so the stored value always matches how the app counts.
    /// </summary>
    public decimal Days { get; set; }

    [Required, MaxLength(300)]
    public string Reason { get; set; } = string.Empty;

    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

    [MaxLength(400)]
    public string? DecisionNote { get; set; }

    public string? DecidedById { get; set; }

    public DateTime? DecidedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}