using System.ComponentModel.DataAnnotations;
using IForm.Web.Models;

namespace IForm.Web.ViewModels;

public class PerformanceListViewModel
{
    public List<PerformanceReview> Reviews { get; set; } = [];

    public PerformanceStatus? Status { get; set; }

    public PerformanceRating? Rating { get; set; }

    public string? Search { get; set; }

    public int TotalCount { get; set; }
}

public class PerformanceFormViewModel
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    [Required]
    [MaxLength(120)]
    public string ReviewPeriod { get; set; } = string.Empty;

    public PerformanceRating Rating { get; set; } = PerformanceRating.Unrated;

    public PerformanceStatus Status { get; set; } = PerformanceStatus.Draft;

    [MaxLength(1000)]
    public string? Goals { get; set; }

    [MaxLength(1000)]
    public string? Feedback { get; set; }

    [MaxLength(1000)]
    public string? Improvements { get; set; }

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    public List<Employee> Employees { get; set; } = [];
}
