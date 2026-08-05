using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class Notification
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Body { get; set; } = string.Empty;

    public int? SiteQueryId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser? User { get; set; }

    public SiteQuery? SiteQuery { get; set; }
}
