using System.ComponentModel.DataAnnotations;
using IForm.Web.Models;

namespace IForm.Web.ViewModels;

public class EngagementListViewModel
{
    public List<EngagementSurvey> Surveys { get; set; } = [];

    public EngagementStatus? Status { get; set; }

    public string? Search { get; set; }

    public int TotalCount { get; set; }
}

public class EngagementFormViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public EngagementStatus Status { get; set; } = EngagementStatus.Draft;

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }
}
