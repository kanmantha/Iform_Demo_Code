using IForm.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IForm.Web.ViewModels;

public class IncidentListViewModel
{
    public string? Search { get; set; }

    public IncidentType? Type { get; set; }

    public IncidentSeverity? Severity { get; set; }

    public IncidentStatus? Status { get; set; }

    public int? SiteId { get; set; }

    public string SortBy { get; set; } = "occurred_desc";

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public IReadOnlyList<Incident> Incidents { get; set; } = new List<Incident>();

    public IReadOnlyList<Site> Sites { get; set; } = new List<Site>();

    public IEnumerable<SelectListItem> TypeOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> SeverityOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
}

public class IncidentFormViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public IncidentType Type { get; set; }

    public IncidentSeverity Severity { get; set; } = IncidentSeverity.Medium;

    public IncidentStatus Status { get; set; } = IncidentStatus.New;

    public DateTime OccurredAt { get; set; } = DateTime.Now;

    public string? Location { get; set; }

    public int? SiteId { get; set; }

    public string? AssignedToId { get; set; }

    public string? ImmediateActionTaken { get; set; }

    public bool IsWorkRelated { get; set; } = true;

    public int? PersonsInvolved { get; set; }

    public int? DaysLost { get; set; }

    public IEnumerable<SelectListItem> TypeOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> SeverityOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> Sites { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> Users { get; set; } = new List<SelectListItem>();
}

public class IncidentDetailViewModel
{
    public Incident Incident { get; set; } = new();

    public bool CanManage { get; set; }

    public IEnumerable<SelectListItem> Users { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> Sites { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
}
