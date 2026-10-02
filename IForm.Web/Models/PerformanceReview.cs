using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class PerformanceReview
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    [Required, MaxLength(120)]
    public string ReviewPeriod { get; set; } = string.Empty;

    public PerformanceRating Rating { get; set; } = PerformanceRating.Unrated;
    public PerformanceStatus Status { get; set; } = PerformanceStatus.Draft;

    [MaxLength(1000)]
    public string? Goals { get; set; }
    [MaxLength(1000)]
    public string? Feedback { get; set; }
    [MaxLength(1000)]
    public string? Improvements { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedById { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
