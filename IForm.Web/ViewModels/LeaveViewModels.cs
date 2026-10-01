using System.ComponentModel.DataAnnotations;
using IForm.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IForm.Web.ViewModels;

public class LeaveListViewModel
{
    public List<LeaveRequest> LeaveRequests { get; set; } = [];

    public List<Employee> Employees { get; set; } = [];

    public LeaveStatus? Status { get; set; }

    public LeaveType? LeaveType { get; set; }

    public int? EmployeeId { get; set; }

    public int TotalCount { get; set; }

    public int PendingCount { get; set; }

    public int ApprovedCount { get; set; }

    public int RejectedCount { get; set; }
}

public class LeaveRequestFormViewModel
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    [Required]
    public LeaveType LeaveType { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

    [Required, DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.UtcNow.Date;

    [Required, MaxLength(300)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>Read-only summary of the inclusive working-day count.</summary>
    public decimal Days { get; set; }

    /// <summary>Balance available to the selected employee, shown before submitting.</summary>
    public decimal AvailableBalance { get; set; }

    public List<Employee> Employees { get; set; } = [];

    public IEnumerable<SelectListItem> EmployeeOptions => Employees
        .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = $"{e.FullName} ({e.EmployeeCode})" });
}

public class LeaveDetailViewModel
{
    public LeaveRequest Request { get; set; } = null!;

    public decimal BalanceBeforeDecision { get; set; }

    public List<LeaveLedgerRow> RecentLedger { get; set; } = [];
}

public sealed record LeaveLedgerRow(DateTime CreatedAt, LeaveType LeaveType, decimal Days, string Reason);