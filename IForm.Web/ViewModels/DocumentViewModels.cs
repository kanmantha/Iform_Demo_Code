using System.ComponentModel.DataAnnotations;
using IForm.Web.Models;

namespace IForm.Web.ViewModels;

public class DocumentListViewModel
{
    public List<Document> Documents { get; set; } = [];

    public DocumentCategory? Category { get; set; }

    public DocumentVisibility? Visibility { get; set; }

    public string? Search { get; set; }

    public int TotalCount { get; set; }
}

public class DocumentFormViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? FileType { get; set; }

    public long FileSize { get; set; }

    public DocumentCategory Category { get; set; } = DocumentCategory.General;

    public DocumentVisibility Visibility { get; set; } = DocumentVisibility.Internal;

    public int? EmployeeId { get; set; }

    public string? UploadedById { get; set; }

    public List<Employee> Employees { get; set; } = [];
}
