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
public class TicketsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public TicketsController(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(
        string? search = null,
        TicketStatus? status = null,
        TicketPriority? priority = null,
        TicketCategory? category = null,
        string? assignedToId = null,
        int? siteId = null,
        string sortBy = "created_desc",
        int page = 1,
        int? pageSize = null)
    {
        const int defaultPageSize = 10;

        var query = _context.Tickets
            .AsNoTracking()
            .Include(t => t.ReportedBy)
            .Include(t => t.AssignedTo)
            .Include(t => t.Site)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(t =>
                t.TicketNumber.Contains(search) ||
                t.Title.Contains(search) ||
                t.Description.Contains(search) ||
                t.Location!.Contains(search));
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(t => t.Priority == priority.Value);
        }

        if (category.HasValue)
        {
            query = query.Where(t => t.Category == category.Value);
        }

        if (!string.IsNullOrEmpty(assignedToId))
        {
            query = query.Where(t => t.AssignedToId == assignedToId);
        }

        if (siteId.HasValue)
        {
            query = query.Where(t => t.SiteId == siteId.Value);
        }

        query = sortBy switch
        {
            "created_asc" => query.OrderBy(t => t.CreatedAt),
            "title" => query.OrderBy(t => t.Title),
            "priority" => query.OrderByDescending(t => t.Priority),
            "updated_desc" => query.OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt),
            _ => query.OrderByDescending(t => t.CreatedAt)
        };

        page = Math.Max(1, page);
        var actualPageSize = pageSize ?? defaultPageSize;
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)actualPageSize);

        var tickets = await query
            .Skip((page - 1) * actualPageSize)
            .Take(actualPageSize)
            .ToListAsync();

        var users = await _context.Users.AsNoTracking().OrderBy(u => u.FullName).ToListAsync();
        var sites = await _context.Sites.AsNoTracking().OrderBy(s => s.Name).ToListAsync();

        var model = new TicketListViewModel
        {
            Search = search,
            Status = status,
            Priority = priority,
            Category = category,
            AssignedToId = assignedToId,
            SiteId = siteId?.ToString(),
            SortBy = sortBy,
            Page = page,
            PageSize = actualPageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Tickets = tickets,
            Users = users,
            Sites = sites,
            StatusOptions = Enum.GetValues<TicketStatus>().Select(s => new SelectListItem(s.ToString(), s.ToString(), status == s)),
            PriorityOptions = Enum.GetValues<TicketPriority>().Select(p => new SelectListItem(p.ToString(), p.ToString(), priority == p)),
            CategoryOptions = Enum.GetValues<TicketCategory>().Select(c => new SelectListItem(c.ToString(), c.ToString(), category == c))
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Count()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Json(new { count = 0 });
        }

        var count = await _context.Tickets.CountAsync(t => t.AssignedToId == user.Id && t.Status != TicketStatus.Closed);
        return Json(new { count });
    }

    public async Task<IActionResult> Board(string? search = null, TicketPriority? priority = null)
    {
        var query = _context.Tickets
            .AsNoTracking()
            .Include(t => t.ReportedBy)
            .Include(t => t.AssignedTo)
            .Include(t => t.Site)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(t =>
                t.TicketNumber.Contains(search) ||
                t.Title.Contains(search) ||
                t.Description.Contains(search));
        }

        if (priority.HasValue)
        {
            query = query.Where(t => t.Priority == priority.Value);
        }

        var tickets = await query.ToListAsync();

        var columns = Enum.GetValues<TicketStatus>()
            .Select(status => new BoardColumn
            {
                Status = status,
                Title = status.ToString(),
                Tickets = tickets
                    .Where(t => t.Status == status)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToList()
            })
            .ToList();

        var model = new TicketBoardViewModel
        {
            Columns = columns,
            Search = search,
            Priority = priority,
            TotalCount = tickets.Count,
            CanMove = User.IsInRole(DbSeeder.AdminRole) || User.IsInRole(DbSeeder.ManagerRole)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Move(int id, TicketStatus status)
    {
        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        var oldStatus = ticket.Status;
        ticket.Status = status;
        ticket.UpdatedAt = DateTime.UtcNow;

        if (status == TicketStatus.Closed)
        {
            ticket.ClosedAt = DateTime.UtcNow;
        }

        if (user is not null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Action = "Update",
                EntityType = "Ticket",
                EntityId = ticket.TicketNumber,
                Details = $"Ticket moved from {oldStatus} to {status} on the board",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        var userId = user?.Id;
        var openCount = await _context.Tickets.CountAsync(t =>
            t.AssignedToId == userId && t.Status != TicketStatus.Closed);

        return Json(new { ok = true, oldStatus = oldStatus.ToString(), status = status.ToString(), count = openCount });
    }

    public async Task<IActionResult> Details(int id)
    {
        var ticket = await _context.Tickets
            .Include(t => t.ReportedBy)
            .Include(t => t.AssignedTo)
            .Include(t => t.Site)
            .Include(t => t.Comments)
                .ThenInclude(c => c.User)
            .Include(t => t.Actions)
                .ThenInclude(a => a.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null)
        {
            return NotFound();
        }

        var users = await _context.Users.AsNoTracking().OrderBy(u => u.FullName).ToListAsync();

        var model = new TicketDetailViewModel
        {
            Ticket = ticket,
            CanAssign = User.IsInRole(DbSeeder.AdminRole) || User.IsInRole(DbSeeder.ManagerRole),
            Users = users.Select(u => new SelectListItem($"{u.FullName} ({u.Email})", u.Id, u.Id == ticket.AssignedToId))
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new TicketFormViewModel { CategoryOptions = CategoryOptions(), PriorityOptions = PriorityOptions(), StatusOptions = StatusOptions(), Sites = SiteOptions() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TicketFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.CategoryOptions = CategoryOptions();
            model.PriorityOptions = PriorityOptions();
            model.StatusOptions = StatusOptions();
            model.Users = await UserOptionsAsync();
            model.Sites = SiteOptions();
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var nextNumber = (await _context.Tickets.MaxAsync(t => (int?)t.Id)) + 1 ?? 1;

        var ticket = new Ticket
        {
            TicketNumber = $"TKT-{nextNumber:D4}",
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            Category = model.Category,
            Priority = model.Priority,
            Status = model.Status,
            Location = model.Location,
            SiteId = model.SiteId,
            DueDate = model.DueDate,
            ReportedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        if (User.IsInRole(DbSeeder.AdminRole) || User.IsInRole(DbSeeder.ManagerRole))
        {
            ticket.AssignedToId = model.AssignedToId;
        }

        _context.Tickets.Add(ticket);
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            UserName = user.FullName,
            Action = "Create",
            EntityType = "Ticket",
            EntityId = ticket.TicketNumber,
            Details = $"Ticket {ticket.TicketNumber} - {ticket.Title}",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Ticket {ticket.TicketNumber} created successfully.";
        return RedirectToAction(nameof(Details), new { id = ticket.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var ticket = await _context.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null)
        {
            return NotFound();
        }

        var model = new TicketFormViewModel
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Category = ticket.Category,
            Priority = ticket.Priority,
            Status = ticket.Status,
            Location = ticket.Location,
            SiteId = ticket.SiteId,
            DueDate = ticket.DueDate,
            AssignedToId = ticket.AssignedToId,
            CategoryOptions = CategoryOptions(),
            PriorityOptions = PriorityOptions(),
            StatusOptions = StatusOptions(),
            Users = await UserOptionsAsync(),
            Sites = SiteOptions()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TicketFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.CategoryOptions = CategoryOptions();
            model.PriorityOptions = PriorityOptions();
            model.StatusOptions = StatusOptions();
            model.Users = await UserOptionsAsync();
            model.Sites = SiteOptions();
            return View(model);
        }

        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == model.Id);
        if (ticket is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        var oldStatus = ticket.Status;

        ticket.Title = model.Title.Trim();
        ticket.Description = model.Description.Trim();
        ticket.Category = model.Category;
        ticket.Priority = model.Priority;
        ticket.Location = model.Location;
        ticket.SiteId = model.SiteId;
        ticket.DueDate = model.DueDate;

        if (User.IsInRole(DbSeeder.AdminRole) || User.IsInRole(DbSeeder.ManagerRole))
        {
            ticket.Status = model.Status;
            ticket.AssignedToId = model.AssignedToId;
            if (model.Status == TicketStatus.Closed)
            {
                ticket.ClosedAt ??= DateTime.UtcNow;
            }
        }

        ticket.UpdatedAt = DateTime.UtcNow;

        if (oldStatus != ticket.Status && user is not null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Action = "Update",
                EntityType = "Ticket",
                EntityId = ticket.TicketNumber,
                Details = $"Status changed from {oldStatus} to {ticket.Status}",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Ticket updated successfully.";
        return RedirectToAction(nameof(Details), new { id = ticket.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ChangeStatus(int id, TicketStatus status, string? resolutionNotes)
    {
        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        var oldStatus = ticket.Status;
        ticket.Status = status;
        ticket.UpdatedAt = DateTime.UtcNow;

        if (status == TicketStatus.Closed)
        {
            ticket.ClosedAt = DateTime.UtcNow;
        }

        if (!string.IsNullOrWhiteSpace(resolutionNotes))
        {
            ticket.ResolutionNotes = resolutionNotes.Trim();
        }

        if (user is not null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Action = "Update",
                EntityType = "Ticket",
                EntityId = ticket.TicketNumber,
                Details = $"Status changed from {oldStatus} to {status}",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Ticket {ticket.TicketNumber} moved to {status}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Assign(int id, string assignedToId)
    {
        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        var assignee = await _userManager.FindByIdAsync(assignedToId ?? string.Empty);
        if (assignee is null)
        {
            ModelState.AddModelError(string.Empty, "Please select a valid assignee.");
            return RedirectToAction(nameof(Details), new { id });
        }

        ticket.AssignedToId = assignee.Id;
        ticket.UpdatedAt = DateTime.UtcNow;

        if (user is not null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Action = "Assign",
                EntityType = "Ticket",
                EntityId = ticket.TicketNumber,
                Details = $"Ticket assigned to {assignee.FullName}",
                CreatedAt = DateTime.UtcNow
            });
        }

        _context.TicketComments.Add(new TicketComment
        {
            TicketId = ticket.Id,
            UserId = user?.Id ?? string.Empty,
            Body = $"Ticket assigned to {assignee.FullName}.",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Ticket assigned to {assignee.FullName}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int id, string? newComment)
    {
        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(newComment))
        {
            var user = await _userManager.GetUserAsync(User);
            _context.TicketComments.Add(new TicketComment
            {
                TicketId = ticket.Id,
                UserId = user?.Id ?? string.Empty,
                Body = newComment.Trim(),
                CreatedAt = DateTime.UtcNow
            });
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Comment added.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private static IEnumerable<SelectListItem> StatusOptions(TicketStatus? selected = null)
        => Enum.GetValues<TicketStatus>().Select(s => new SelectListItem(s.ToString(), s.ToString(), s == selected));

    private static IEnumerable<SelectListItem> PriorityOptions(TicketPriority? selected = null)
        => Enum.GetValues<TicketPriority>().Select(p => new SelectListItem(p.ToString(), p.ToString(), p == selected));

    private static IEnumerable<SelectListItem> CategoryOptions(TicketCategory? selected = null)
        => Enum.GetValues<TicketCategory>().Select(c => new SelectListItem(c.ToString(), c.ToString(), c == selected));

    private async Task<IEnumerable<SelectListItem>> UserOptionsAsync(string? selectedId = null)
    {
        var users = await _context.Users.AsNoTracking().OrderBy(u => u.FullName).ToListAsync();
        return users.Select(u => new SelectListItem($"{u.FullName} ({u.Email})", u.Id, u.Id == selectedId));
    }

    private IEnumerable<SelectListItem> SiteOptions(int? selectedId = null)
    {
        var sites = _context.Sites.AsNoTracking().OrderBy(s => s.Name).ToList();
        return sites.Select(s => new SelectListItem($"{s.Name} - {s.Code}", s.Id.ToString(), s.Id == selectedId));
    }
}
