using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class QueryComment
{
    public int Id { get; set; }

    public int SiteQueryId { get; set; }

    [Required]
    public string Body { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SiteQuery? SiteQuery { get; set; }

    public AppUser? User { get; set; }
}
