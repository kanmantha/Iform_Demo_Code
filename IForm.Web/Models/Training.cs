using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class Training
{
    public int Id { get; set; }

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Description { get; set; }
    public TrainingType Type { get; set; } = TrainingType.Optional;
    public TrainingStatus Status { get; set; } = TrainingStatus.NotStarted;
    public int? DurationHours { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
