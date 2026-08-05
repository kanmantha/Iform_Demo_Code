using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize]
public class ActionsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public ActionsController(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(
        string? search,
        ActionStatus? status,
        ActionPriority? priority,
        string? assignedToId,
        bool overdueOnly = false,
        string sortBy = "due_asc",
        int page = 1)
    {
        const int pageSize = 10;
        var now = DateTime.UtcNow;

        var query = _context.ActionItems
            .AsNoTracking()
            .Include(a => a.AssignedTo)
            .Include(a => a.Incident)
            .Include(a => a.Ticket)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(a =>
                a.ActionNumber.Contains(search) ||
                a.Title.Contains(search) ||
                a.Description.Contains(search));
        }

        if (status.HasValue) query = query.Where(a => a.Status == status.Value);
        if (priority.HasValue) query = query.Where(a => a.Priority == priority.Value);
        if (!string.IsNullOrEmpty(assignedToId)) query = query.Where(a => a.AssignedToId == assignedToId);
        if (overdueOnly) query = query.Where(a => a.Status != ActionStatus.Completed && a.Status != ActionStatus.Cancelled && a.DueDate < now);

        query = sortBy switch
        {
            "due_desc" => query.OrderByDescending(a => a.DueDate ?? DateTime.MaxValue),
            "priority" => query.OrderByDescending(a => a.Priority),
            "created_desc" => query.OrderByDescending(a => a.CreatedAt),
            _ => query.OrderBy(a => a.DueDate ?? DateTime.MaxValue)
        };

        page = Math.Max(1, page);
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var actions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var overdueCount = await _context.ActionItems.CountAsync(a =>
            a.Status != ActionStatus.Completed && a.Status != ActionStatus.Cancelled && a.DueDate < now);

        var users = await _context.Users.AsNoTracking().OrderBy(u => u.FullName).ToListAsync();

        var model = new ActionListViewModel
        {
            Search = search,
            Status = status,
            Priority = priority,
            AssignedToId = assignedToId,
            OverdueOnly = overdueOnly,
            SortBy = sortBy,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            OverdueCount = overdueCount,
            Actions = actions,
            Users = users,
            StatusOptions = Enum.GetValues<ActionStatus>().Select(s => new SelectListItem(s.ToString(), s.ToString(), status == s)),
            PriorityOptions = Enum.GetValues<ActionPriority>().Select(p => new SelectListItem(p.ToString(), p.ToString(), priority == p))
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var action = await _context.ActionItems
            .Include(a => a.AssignedTo)
            .Include(a => a.CreatedBy)
            .Include(a => a.Incident)
            .Include(a => a.Ticket)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (action is null)
        {
            return NotFound();
        }

        var model = new ActionDetailViewModel
        {
            Action = action,
            CanManage = User.IsInRole(DbSeeder.AdminRole) || User.IsInRole(DbSeeder.ManagerRole)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? incidentId, int? ticketId)
    {
        var currentUser = await _userManager.GetUserAsync(User);

        return View(new ActionFormViewModel
        {
            IncidentId = incidentId,
            TicketId = ticketId,
            AssignedToId = currentUser?.Id ?? string.Empty,
            PriorityOptions = PriorityOptions(),
            StatusOptions = StatusOptions(),
            Users = await UserOptionsAsync(currentUser?.Id),
            Incidents = await IncidentOptionsAsync(incidentId),
            Tickets = await TicketOptionsAsync(ticketId)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActionFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.PriorityOptions = PriorityOptions();
            model.StatusOptions = StatusOptions();
            model.Users = await UserOptionsAsync();
            model.Incidents = await IncidentOptionsAsync(model.IncidentId);
            model.Tickets = await TicketOptionsAsync(model.TicketId);
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var nextNumber = (await _context.ActionItems.MaxAsync(a => (int?)a.Id)) + 1 ?? 1;

        var action = new ActionItem
        {
            ActionNumber = $"ACT-{nextNumber:D3}",
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            Priority = model.Priority,
            Status = model.Status,
            DueDate = model.DueDate.HasValue ? DateTime.SpecifyKind(model.DueDate.Value, DateTimeKind.Local).ToUniversalTime() : null,
            AssignedToId = model.AssignedToId,
            IncidentId = model.IncidentId,
            TicketId = model.TicketId,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.ActionItems.Add(action);
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            UserName = user.FullName,
            Action = "Create",
            EntityType = "Action",
            EntityId = action.ActionNumber,
            Details = $"Action {action.ActionNumber} - {action.Title}",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Action {action.ActionNumber} created.";
        return RedirectToAction(nameof(Details), new { id = action.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var action = await _context.ActionItems.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        if (action is null)
        {
            return NotFound();
        }

        var model = new ActionFormViewModel
        {
            Id = action.Id,
            Title = action.Title,
            Description = action.Description,
            Priority = action.Priority,
            Status = action.Status,
            DueDate = action.DueDate?.ToLocalTime(),
            AssignedToId = action.AssignedToId,
            IncidentId = action.IncidentId,
            TicketId = action.TicketId,
            CompletionNotes = action.CompletionNotes,
            PriorityOptions = PriorityOptions(),
            StatusOptions = StatusOptions(),
            Users = await UserOptionsAsync(action.AssignedToId),
            Incidents = await IncidentOptionsAsync(action.IncidentId),
            Tickets = await TicketOptionsAsync(action.TicketId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(ActionFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.PriorityOptions = PriorityOptions();
            model.StatusOptions = StatusOptions();
            model.Users = await UserOptionsAsync(model.AssignedToId);
            model.Incidents = await IncidentOptionsAsync(model.IncidentId);
            model.Tickets = await TicketOptionsAsync(model.TicketId);
            return View(model);
        }

        var action = await _context.ActionItems.FirstOrDefaultAsync(a => a.Id == model.Id);
        if (action is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        var oldStatus = action.Status;

        action.Title = model.Title.Trim();
        action.Description = model.Description.Trim();
        action.Priority = model.Priority;
        action.Status = model.Status;
        action.DueDate = model.DueDate.HasValue ? DateTime.SpecifyKind(model.DueDate.Value, DateTimeKind.Local).ToUniversalTime() : null;
        action.AssignedToId = model.AssignedToId;
        action.IncidentId = model.IncidentId;
        action.TicketId = model.TicketId;
        action.CompletionNotes = model.CompletionNotes;
        action.UpdatedAt = DateTime.UtcNow;

        if (model.Status == ActionStatus.Completed)
        {
            action.CompletedAt ??= DateTime.UtcNow;
        }
        else
        {
            action.CompletedAt = null;
        }

        if (oldStatus != action.Status && user is not null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Action = "Update",
                EntityType = "Action",
                EntityId = action.ActionNumber,
                Details = $"Status changed from {oldStatus} to {action.Status}",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Action updated successfully.";
        return RedirectToAction(nameof(Details), new { id = action.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string? completionNotes)
    {
        var action = await _context.ActionItems.FirstOrDefaultAsync(a => a.Id == id);
        if (action is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        action.Status = ActionStatus.Completed;
        action.CompletedAt = DateTime.UtcNow;
        action.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(completionNotes))
        {
            action.CompletionNotes = completionNotes.Trim();
        }

        if (user is not null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Action = "Complete",
                EntityType = "Action",
                EntityId = action.ActionNumber,
                Details = $"Action {action.ActionNumber} completed",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Action {action.ActionNumber} marked as completed.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ChangeStatus(int id, ActionStatus status)
    {
        var action = await _context.ActionItems.FirstOrDefaultAsync(a => a.Id == id);
        if (action is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        var oldStatus = action.Status;
        action.Status = status;
        action.UpdatedAt = DateTime.UtcNow;

        if (status == ActionStatus.Completed)
        {
            action.CompletedAt ??= DateTime.UtcNow;
        }
        else
        {
            action.CompletedAt = null;
        }

        if (user is not null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Action = "Update",
                EntityType = "Action",
                EntityId = action.ActionNumber,
                Details = $"Status changed from {oldStatus} to {status}",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Action {action.ActionNumber} moved to {status}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private static IEnumerable<SelectListItem> PriorityOptions(ActionPriority? selected = null)
        => Enum.GetValues<ActionPriority>().Select(p => new SelectListItem(p.ToString(), p.ToString(), p == selected));

    private static IEnumerable<SelectListItem> StatusOptions(ActionStatus? selected = null)
        => Enum.GetValues<ActionStatus>().Select(s => new SelectListItem(s.ToString(), s.ToString(), s == selected));

    private async Task<IEnumerable<SelectListItem>> UserOptionsAsync(string? selectedId = null)
    {
        var users = await _context.Users.AsNoTracking().OrderBy(u => u.FullName).ToListAsync();
        return users.Select(u => new SelectListItem($"{u.FullName} ({u.Email})", u.Id, u.Id == selectedId));
    }

    private async Task<IEnumerable<SelectListItem>> IncidentOptionsAsync(int? selectedId = null)
    {
        var incidents = await _context.Incidents.AsNoTracking().OrderByDescending(i => i.OccurredAt).Take(50).ToListAsync();
        return incidents.Select(i => new SelectListItem($"{i.IncidentNumber} - {i.Title}", i.Id.ToString(), i.Id == selectedId));
    }

    private async Task<IEnumerable<SelectListItem>> TicketOptionsAsync(int? selectedId = null)
    {
        var tickets = await _context.Tickets.AsNoTracking().OrderByDescending(t => t.CreatedAt).Take(50).ToListAsync();
        return tickets.Select(t => new SelectListItem($"{t.TicketNumber} - {t.Title}", t.Id.ToString(), t.Id == selectedId));
    }
}
