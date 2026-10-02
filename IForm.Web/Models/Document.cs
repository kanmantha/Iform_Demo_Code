using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class Document
{
    public int Id { get; set; }

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Description { get; set; }
    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;
    [MaxLength(120)]
    public string? FileType { get; set; }
    public long FileSize { get; set; }
    public DocumentCategory Category { get; set; } = DocumentCategory.General;
    public DocumentVisibility Visibility { get; set; } = DocumentVisibility.Internal;
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string? UploadedById { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
