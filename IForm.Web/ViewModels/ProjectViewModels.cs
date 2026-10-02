using System.ComponentModel.DataAnnotations;
using IForm.Web.Models;

namespace IForm.Web.ViewModels;

public class ProjectListViewModel
{
    public List<Project> Projects { get; set; } = [];

    public ProjectStatus? Status { get; set; }

    public string? Search { get; set; }

    public int TotalCount { get; set; }

    public int PlannedCount { get; set; }

    public int ActiveCount { get; set; }

    public int OnHoldCount { get; set; }

    public int CompletedCount { get; set; }

    public int CancelledCount { get; set; }
}

public class ProjectFormViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Planned;

    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }
}
