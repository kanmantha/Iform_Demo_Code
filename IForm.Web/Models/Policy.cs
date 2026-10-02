using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class Policy
{
    public int Id { get; set; }

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Description { get; set; }
    [MaxLength(500)]
    public string? FilePath { get; set; }
    public PolicyStatus Status { get; set; } = PolicyStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public string? PublishedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
