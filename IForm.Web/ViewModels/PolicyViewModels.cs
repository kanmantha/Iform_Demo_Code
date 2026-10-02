using System.ComponentModel.DataAnnotations;
using IForm.Web.Models;

namespace IForm.Web.ViewModels;

public class PolicyListViewModel
{
    public List<Policy> Policies { get; set; } = [];

    public PolicyStatus? Status { get; set; }

    public string? Search { get; set; }

    public int TotalCount { get; set; }

    public int DraftCount { get; set; }

    public int PublishedCount { get; set; }

    public int ArchivedCount { get; set; }
}

public class PolicyFormViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? FilePath { get; set; }

    public PolicyStatus Status { get; set; } = PolicyStatus.Draft;
}
