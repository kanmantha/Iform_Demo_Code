using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class Product
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string ProductCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Family { get; set; }

    [MaxLength(100)]
    public string? Material { get; set; }

    [MaxLength(500)]
    public string? Specification { get; set; }

    [MaxLength(200)]
    public string? Dimensions { get; set; }

    [MaxLength(200)]
    public string? Project { get; set; }

    [MaxLength(50)]
    public string? Unit { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SiteQuery> SiteQueries { get; set; } = new List<SiteQuery>();
}
