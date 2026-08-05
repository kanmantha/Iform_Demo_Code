using System.ComponentModel.DataAnnotations;
using IForm.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IForm.Web.ViewModels;

public class DispatchListViewModel
{
    public string? Search { get; set; }

    public DispatchStatus? Status { get; set; }

    public QueryCategory? Reason { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public IReadOnlyList<DispatchOrder> DispatchOrders { get; set; } = new List<DispatchOrder>();

    public int PendingCount { get; set; }

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> ReasonOptions { get; set; } = new List<SelectListItem>();
}

public class DispatchFormViewModel
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string? DispatchNumber { get; set; }

    [Required, MaxLength(30), Display(Name = "IPO number")]
    public string IpoNumber { get; set; } = string.Empty;

    [Required, MaxLength(200), Display(Name = "Project")]
    public string Project { get; set; } = string.Empty;

    [Display(Name = "Slab target date")]
    public DateTime? SlabTargetDate { get; set; }

    [Display(Name = "Slab completed date")]
    public DateTime? SlabCompletedDate { get; set; }

    [Display(Name = "Slab delay (days)")]
    public int? SlabDelayDays { get; set; }

    [Display(Name = "Qty (nos)")]
    public int? QuantityNos { get; set; }

    [Display(Name = "Qty (sqm)")]
    public decimal? QuantitySqm { get; set; }

    [Display(Name = "Casting date")]
    public DateTime? CastingDate { get; set; }

    [Display(Name = "Dispatch status")]
    public DispatchStatus DispatchStatus { get; set; } = DispatchStatus.Pending;

    [Display(Name = "Dispatch date")]
    public DateTime? DispatchDate { get; set; }

    [Display(Name = "Delay (days)")]
    public int? DelayDays { get; set; }

    [Display(Name = "Reason")]
    public QueryCategory? Reason { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> ReasonOptions { get; set; } = new List<SelectListItem>();
}

public class DispatchDetailViewModel
{
    public DispatchOrder DispatchOrder { get; set; } = null!;

    public bool CanEdit { get; set; }
}

public class MaterialCertificateListViewModel
{
    public string? Search { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public IReadOnlyList<MaterialCertificate> Certificates { get; set; } = new List<MaterialCertificate>();
}

public class MaterialCertificateFormViewModel
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string? CertificateNumber { get; set; }

    [Required, MaxLength(100), Display(Name = "Supplier")]
    public string Supplier { get; set; } = string.Empty;

    [MaxLength(50), Display(Name = "Supplier report no / Lab no")]
    public string? SupplierReportNo { get; set; }

    [MaxLength(50), Display(Name = "Work order no")]
    public string? WorkOrderNo { get; set; }

    [MaxLength(100), Display(Name = "Alloy / grade")]
    public string? Alloy { get; set; }

    [MaxLength(100), Display(Name = "Test method")]
    public string? TestMethod { get; set; }

    [Display(Name = "Report date")]
    public DateTime? ReportDate { get; set; }

    [Display(Name = "Sample received date")]
    public DateTime? SampleReceivedDate { get; set; }

    [Display(Name = "Test date")]
    public DateTime? TestDate { get; set; }

    [Display(Name = "Valid until")]
    public DateTime? ValidUntil { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }
}

public class MaterialCertificateDetailViewModel
{
    public MaterialCertificate Certificate { get; set; } = null!;

    public bool CanEdit { get; set; }
}
