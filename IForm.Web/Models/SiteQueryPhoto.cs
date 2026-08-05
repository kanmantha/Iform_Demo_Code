using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class SiteQueryPhoto
{
    public int Id { get; set; }

    public int SiteQueryId { get; set; }

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Caption { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public SiteQuery? SiteQuery { get; set; }
}
