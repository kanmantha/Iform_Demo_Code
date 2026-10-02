using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class LmsCourse
{
    public int Id { get; set; }

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Description { get; set; }
    public LmsStatus Status { get; set; } = LmsStatus.NotStarted;
    public int? DurationHours { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
