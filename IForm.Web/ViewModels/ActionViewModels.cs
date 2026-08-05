using IForm.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IForm.Web.ViewModels;

public class ActionListViewModel
{
    public string? Search { get; set; }

    public ActionStatus? Status { get; set; }

    public ActionPriority? Priority { get; set; }

    public string? AssignedToId { get; set; }

    public bool OverdueOnly { get; set; }

    public string SortBy { get; set; } = "due_asc";

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public int OverdueCount { get; set; }

    public IReadOnlyList<ActionItem> Actions { get; set; } = new List<ActionItem>();

    public IReadOnlyList<AppUser> Users { get; set; } = new List<AppUser>();

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> PriorityOptions { get; set; } = new List<SelectListItem>();
}

public class ActionFormViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ActionPriority Priority { get; set; } = ActionPriority.Medium;

    public ActionStatus Status { get; set; } = ActionStatus.Open;

    public DateTime? DueDate { get; set; }

    public string AssignedToId { get; set; } = string.Empty;

    public int? IncidentId { get; set; }

    public int? TicketId { get; set; }

    public string? CompletionNotes { get; set; }

    public IEnumerable<SelectListItem> PriorityOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> Users { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> Incidents { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> Tickets { get; set; } = new List<SelectListItem>();
}

public class ActionDetailViewModel
{
    public ActionItem Action { get; set; } = new();

    public bool CanManage { get; set; }
}
