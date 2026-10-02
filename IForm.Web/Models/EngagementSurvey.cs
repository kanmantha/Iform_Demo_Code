using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class EngagementSurvey
{
    public int Id { get; set; }

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Description { get; set; }
    public EngagementStatus Status { get; set; } = EngagementStatus.Draft;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
