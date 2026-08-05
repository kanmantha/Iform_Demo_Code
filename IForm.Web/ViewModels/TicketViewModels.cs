using IForm.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IForm.Web.ViewModels;

public class TicketListViewModel
{
    public string? Search { get; set; }

    public TicketStatus? Status { get; set; }

    public TicketPriority? Priority { get; set; }

    public TicketCategory? Category { get; set; }

    public string? AssignedToId { get; set; }

    public string? SiteId { get; set; }

    public string SortBy { get; set; } = "created_desc";

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public IReadOnlyList<Ticket> Tickets { get; set; } = new List<Ticket>();

    public IReadOnlyList<AppUser> Users { get; set; } = new List<AppUser>();

    public IReadOnlyList<Site> Sites { get; set; } = new List<Site>();

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> PriorityOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();
}

public class TicketFormViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public TicketCategory Category { get; set; }

    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    public TicketStatus Status { get; set; } = TicketStatus.New;

    public string? Location { get; set; }

    public int? SiteId { get; set; }

    public DateTime? DueDate { get; set; }

    public string? AssignedToId { get; set; }

    public IEnumerable<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> PriorityOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> Users { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> Sites { get; set; } = new List<SelectListItem>();
}

public class TicketDetailViewModel
{
    public Ticket Ticket { get; set; } = new();

    public bool CanAssign { get; set; }

    public IEnumerable<SelectListItem> Users { get; set; } = new List<SelectListItem>();

    public string? NewComment { get; set; }
}

public class UpdateStatusViewModel
{
    public int Id { get; set; }

    public TicketStatus Status { get; set; }

    public string? ResolutionNotes { get; set; }
}

public class TicketBoardViewModel
{
    public IReadOnlyList<BoardColumn> Columns { get; set; } = new List<BoardColumn>();

    public string? Search { get; set; }

    public TicketPriority? Priority { get; set; }

    public bool CanMove { get; set; }

    public int TotalCount { get; set; }
}

public class BoardColumn
{
    public TicketStatus Status { get; set; }

    public string Title { get; set; } = string.Empty;

    public IReadOnlyList<Ticket> Tickets { get; set; } = new List<Ticket>();
}
