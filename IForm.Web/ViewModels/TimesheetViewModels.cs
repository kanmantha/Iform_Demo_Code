using System.ComponentModel.DataAnnotations;
using IForm.Web.Models;

namespace IForm.Web.ViewModels;

public class TimesheetListViewModel
{
    public List<Timesheet> Timesheets { get; set; } = [];

    public List<Employee> Employees { get; set; } = [];

    public List<Project> Projects { get; set; } = [];

    public int? EmployeeId { get; set; }

    public int? ProjectId { get; set; }

    public TimesheetStatus? Status { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string? Search { get; set; }

    public int TotalCount { get; set; }

    public int DraftCount { get; set; }

    public int SubmittedCount { get; set; }

    public int ApprovedCount { get; set; }

    public int RejectedCount { get; set; }
}

public class TimesheetFormViewModel
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    [Required]
    [Range(0.01, 24, ErrorMessage = "Hours must be between 0.01 and 24")]
    public decimal Hours { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;

    public int? ProjectId { get; set; }

    public List<Employee> Employees { get; set; } = [];

    public List<Project> Projects { get; set; } = [];
}
