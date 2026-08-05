using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class DispatchOrder
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string DispatchNumber { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string IpoNumber { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Project { get; set; } = string.Empty;

    public DateTime? SlabTargetDate { get; set; }

    public DateTime? SlabCompletedDate { get; set; }

    public int? SlabDelayDays { get; set; }

    public int? QuantityNos { get; set; }

    public decimal? QuantitySqm { get; set; }

    public DateTime? CastingDate { get; set; }

    public DispatchStatus DispatchStatus { get; set; } = DispatchStatus.Pending;

    public DateTime? DispatchDate { get; set; }

    public int? DelayDays { get; set; }

    public QueryCategory? Reason { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public string CreatedById { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public AppUser? CreatedBy { get; set; }
}
