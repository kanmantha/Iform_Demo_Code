using System.ComponentModel.DataAnnotations;
using IForm.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IForm.Web.ViewModels;

public class SiteQueryListViewModel
{
    public string? Search { get; set; }

    public string? Project { get; set; }

    public QueryCategory? Category { get; set; }

    public QueryStatus? Status { get; set; }

    public bool Escalated { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public IReadOnlyList<SiteQuery> SiteQueries { get; set; } = new List<SiteQuery>();

    public IEnumerable<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();

    public IReadOnlyList<string> ProjectOptions { get; set; } = new List<string>();
}

public class SiteQueryFormViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    [Display(Name = "IPO number")]
    public string IpoNumber { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    [Display(Name = "Project")]
    public string Project { get; set; } = string.Empty;

    [Display(Name = "Issue type")]
    public QueryCategory Category { get; set; }

    [Range(0.01, 999999, ErrorMessage = "Quantity (nos) is required and must be greater than 0.")]
    [Display(Name = "Quantity (nos)")]
    public decimal QuantityNos { get; set; }

    [Range(0.01, 999999, ErrorMessage = "Quantity (sqm) is required and must be greater than 0.")]
    [Display(Name = "Quantity (sqm)")]
    public decimal QuantitySqm { get; set; }

    [Required]
    [Display(Name = "Issue description")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Verified product code")]
    public int? ProductId { get; set; }

    [Required(ErrorMessage = "Photo evidence is required.")]
    [Display(Name = "Photo evidence")]
    public IFormFile? Photo { get; set; }

    [Display(Name = "Slab target casting date")]
    public DateTime? SlabTargetDate { get; set; }

    [Display(Name = "Slab completed date")]
    public DateTime? SlabCompletedDate { get; set; }

    public IEnumerable<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> ProductOptions { get; set; } = new List<SelectListItem>();

    public IReadOnlyList<string> ProjectOptions { get; set; } = new List<string>();
}

public class SiteQueryDetailViewModel
{
    public SiteQuery SiteQuery { get; set; } = null!;

    public bool CanResolve { get; set; }

    public string? NewComment { get; set; }
}

public class SiteQueryDashboardViewModel
{
    public int TotalQueries { get; set; }

    public int OpenQueries { get; set; }

    public int ResolvedQueries { get; set; }

    public int ResolvedThisMonth { get; set; }

    public int EscalatedQueries { get; set; }

    public double AvgDelayDays { get; set; }

    public IReadOnlyList<ProjectDelay> ProjectDelays { get; set; } = new List<ProjectDelay>();

    public IReadOnlyDictionary<string, int> ByCategory { get; set; } = new Dictionary<string, int>();

    public IReadOnlyList<SiteQuery> OldestOpen { get; set; } = new List<SiteQuery>();

    public IReadOnlyList<RaisedByStat> RaisedByStats { get; set; } = new List<RaisedByStat>();
}

public record ProjectDelay(string Project, int OpenCount, int MaxDelayDays, string? TopCategory);

public record RaisedByStat(string Name, int OpenCount, int MaxDelayDays);
