using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class SiteQuery
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string QueryNumber { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string IpoNumber { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Project { get; set; } = string.Empty;

    public QueryCategory Category { get; set; }

    public QueryStatus Status { get; set; } = QueryStatus.Pending;

    [Required]
    public string Description { get; set; } = string.Empty;

    public decimal QuantityNos { get; set; }

    public decimal QuantitySqm { get; set; }

    public int? ProductId { get; set; }

    [MaxLength(50)]
    public string? ProductCode { get; set; }

    [MaxLength(300)]
    public string? PhotoPath { get; set; }

    public DateTime? SlabTargetDate { get; set; }

    public DateTime? SlabCompletedDate { get; set; }

    public int? SlabDelayDays { get; set; }

    public string RaisedById { get; set; } = string.Empty;

    public string? ResolvedById { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public Product? Product { get; set; }

    public AppUser? RaisedBy { get; set; }

    public AppUser? ResolvedBy { get; set; }

    public ICollection<SiteQueryPhoto> Photos { get; set; } = new List<SiteQueryPhoto>();

    public ICollection<QueryComment> Comments { get; set; } = new List<QueryComment>();

    public int DelayDays =>
        (int)((ResolvedAt ?? DateTime.UtcNow).Date - CreatedAt.Date).TotalDays;
}
