using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class MaterialCertificate
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string CertificateNumber { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Supplier { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? SupplierReportNo { get; set; }

    [MaxLength(50)]
    public string? WorkOrderNo { get; set; }

    [MaxLength(100)]
    public string? Alloy { get; set; }

    [MaxLength(100)]
    public string? TestMethod { get; set; }

    public DateTime? ReportDate { get; set; }

    public DateTime? SampleReceivedDate { get; set; }

    public DateTime? TestDate { get; set; }

    public DateTime? ValidUntil { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    [MaxLength(300)]
    public string? FilePath { get; set; }

    [MaxLength(100)]
    public string? FileName { get; set; }

    public string UploadedById { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public AppUser? UploadedBy { get; set; }
}
