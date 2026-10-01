using System.ComponentModel.DataAnnotations;
using IForm.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IForm.Web.ViewModels;

public class ExpenseListViewModel
{
    public List<ExpenseClaim> Claims { get; set; } = [];

    public List<Employee> Employees { get; set; } = [];

    public ExpenseStatus? Status { get; set; }

    public int? EmployeeId { get; set; }

    public int TotalCount { get; set; }

    public int PendingCount { get; set; }

    public int ApprovedCount { get; set; }

    public int RejectedCount { get; set; }

    public decimal ReimbursedTotal { get; set; }
}

public class ExpenseClaimFormViewModel
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    [Required, MaxLength(120)]
    public string Category { get; set; } = "Travel";

    [MaxLength(200)]
    public string? Description { get; set; }

    [Range(0.01, 10_000_000)]
    public decimal Amount { get; set; }

    [Required, MaxLength(3), RegularExpression("^[A-Za-z]{3}$")]
    public string Currency { get; set; } = "INR";

    [Required, DataType(DataType.Date)]
    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow.Date;

    public List<Employee> Employees { get; set; } = [];

    public IEnumerable<SelectListItem> EmployeeOptions => Employees
        .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = $"{e.FullName} ({e.EmployeeCode})" });
}

public class AttendanceListViewModel
{
    public List<AttendanceRecord> Records { get; set; } = [];

    public List<Employee> Employees { get; set; } = [];

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public int? EmployeeId { get; set; }

    public AttendanceStatus? Status { get; set; }

    public decimal TotalHours { get; set; }

    public int TotalCount { get; set; }
}

public class AttendanceFormViewModel
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;

    [DataType(DataType.DateTime)]
    public DateTime? CheckIn { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? CheckOut { get; set; }

    [Range(0, 16)]
    public decimal HoursWorked { get; set; }

    [MaxLength(300)]
    public string? Notes { get; set; }

    public List<Employee> Employees { get; set; } = [];

    public IEnumerable<SelectListItem> EmployeeOptions => Employees
        .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = $"{e.FullName} ({e.EmployeeCode})" });
}