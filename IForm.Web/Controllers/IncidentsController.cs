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
public class IncidentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public IncidentsController(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(
        string? search,
        IncidentType? type,
        IncidentSeverity? severity,
        IncidentStatus? status,
        int? siteId,
        string sortBy = "occurred_desc",
        int page = 1)
    {
        const int pageSize = 10;

        var query = _context.Incidents
            .AsNoTracking()
            .Include(i => i.ReportedBy)
            .Include(i => i.AssignedTo)
            .Include(i => i.Site)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(i =>
                i.IncidentNumber.Contains(search) ||
                i.Title.Contains(search) ||
                i.Description.Contains(search) ||
                i.Location!.Contains(search));
        }

        if (type.HasValue) query = query.Where(i => i.Type == type.Value);
        if (severity.HasValue) query = query.Where(i => i.Severity == severity.Value);
        if (status.HasValue) query = query.Where(i => i.Status == status.Value);
        if (siteId.HasValue) query = query.Where(i => i.SiteId == siteId.Value);

        query = sortBy switch
        {
            "occurred_asc" => query.OrderBy(i => i.OccurredAt),
            "severity" => query.OrderByDescending(i => i.Severity),
            "status" => query.OrderBy(i => i.Status),
            _ => query.OrderByDescending(i => i.OccurredAt)
        };

        page = Math.Max(1, page);
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var incidents = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var sites = await _context.Sites.AsNoTracking().OrderBy(s => s.Name).ToListAsync();

        var model = new IncidentListViewModel
        {
            Search = search,
            Type = type,
            Severity = severity,
            Status = status,
            SiteId = siteId,
            SortBy = sortBy,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Incidents = incidents,
            Sites = sites,
            TypeOptions = Enum.GetValues<IncidentType>().Select(v => new SelectListItem(v.ToString(), v.ToString(), type == v)),
            SeverityOptions = Enum.GetValues<IncidentSeverity>().Select(v => new SelectListItem(v.ToString(), v.ToString(), severity == v)),
            StatusOptions = Enum.GetValues<IncidentStatus>().Select(v => new SelectListItem(v.ToString(), v.ToString(), status == v))
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var incident = await _context.Incidents
            .Include(i => i.ReportedBy)
            .Include(i => i.AssignedTo)
            .Include(i => i.Site)
            .Include(i => i.Actions)
                .ThenInclude(a => a.AssignedTo)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (incident is null)
        {
            return NotFound();
        }

        var users = await _context.Users.AsNoTracking().OrderBy(u => u.FullName).ToListAsync();
        var sites = await _context.Sites.AsNoTracking().OrderBy(s => s.Name).ToListAsync();

        var model = new IncidentDetailViewModel
        {
            Incident = incident,
            CanManage = User.IsInRole(DbSeeder.AdminRole) || User.IsInRole(DbSeeder.ManagerRole),
            Users = users.Select(u => new SelectListItem($"{u.FullName} ({u.Email})", u.Id, u.Id == incident.AssignedToId)),
            Sites = sites.Select(s => new SelectListItem($"{s.Code} - {s.Name}", s.Id.ToString(), s.Id == incident.SiteId)),
            StatusOptions = Enum.GetValues<IncidentStatus>().Select(s => new SelectListItem(s.ToString(), s.ToString(), s == incident.Status))
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(new IncidentFormViewModel
        {
            TypeOptions = TypeOptions(),
            SeverityOptions = SeverityOptions(),
            StatusOptions = StatusOptions(),
            Sites = await SiteOptionsAsync(),
            Users = await UserOptionsAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IncidentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.TypeOptions = TypeOptions();
            model.SeverityOptions = SeverityOptions();
            model.StatusOptions = StatusOptions();
            model.Sites = await SiteOptionsAsync();
            model.Users = await UserOptionsAsync();
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var nextNumber = (await _context.Incidents.MaxAsync(i => (int?)i.Id)) + 1 ?? 1;

        var incident = new Incident
        {
            IncidentNumber = $"INC-{nextNumber:D3}",
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            Type = model.Type,
            Severity = model.Severity,
            Status = model.Status,
            OccurredAt = DateTime.SpecifyKind(model.OccurredAt, DateTimeKind.Local).ToUniversalTime(),
            Location = model.Location,
            SiteId = model.SiteId,
            ReportedById = user.Id,
            IsWorkRelated = model.IsWorkRelated,
            PersonsInvolved = model.PersonsInvolved,
            DaysLost = model.DaysLost,
            ImmediateActionTaken = model.ImmediateActionTaken,
            CreatedAt = DateTime.UtcNow
        };

        if (User.IsInRole(DbSeeder.AdminRole) || User.IsInRole(DbSeeder.ManagerRole))
        {
            incident.AssignedToId = model.AssignedToId;
        }

        _context.Incidents.Add(incident);
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            UserName = user.FullName,
            Action = "Create",
            EntityType = "Incident",
            EntityId = incident.IncidentNumber,
            Details = $"Incident {incident.IncidentNumber} - {incident.Title}",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Incident {incident.IncidentNumber} reported.";
        return RedirectToAction(nameof(Details), new { id = incident.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var incident = await _context.Incidents.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        if (incident is null)
        {
            return NotFound();
        }

        var model = new IncidentFormViewModel
        {
            Id = incident.Id,
            Title = incident.Title,
            Description = incident.Description,
            Type = incident.Type,
            Severity = incident.Severity,
            Status = incident.Status,
            OccurredAt = incident.OccurredAt.ToLocalTime(),
            Location = incident.Location,
            SiteId = incident.SiteId,
            AssignedToId = incident.AssignedToId,
            ImmediateActionTaken = incident.ImmediateActionTaken,
            IsWorkRelated = incident.IsWorkRelated,
            PersonsInvolved = incident.PersonsInvolved,
            DaysLost = incident.DaysLost,
            TypeOptions = TypeOptions(),
            SeverityOptions = SeverityOptions(),
            StatusOptions = StatusOptions(),
            Sites = await SiteOptionsAsync(incident.SiteId),
            Users = await UserOptionsAsync(incident.AssignedToId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(IncidentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.TypeOptions = TypeOptions();
            model.SeverityOptions = SeverityOptions();
            model.StatusOptions = StatusOptions();
            model.Sites = await SiteOptionsAsync(model.SiteId);
            model.Users = await UserOptionsAsync(model.AssignedToId);
            return View(model);
        }

        var incident = await _context.Incidents.FirstOrDefaultAsync(i => i.Id == model.Id);
        if (incident is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        var oldStatus = incident.Status;

        incident.Title = model.Title.Trim();
        incident.Description = model.Description.Trim();
        incident.Type = model.Type;
        incident.Severity = model.Severity;
        incident.OccurredAt = DateTime.SpecifyKind(model.OccurredAt, DateTimeKind.Local).ToUniversalTime();
        incident.Location = model.Location;
        incident.SiteId = model.SiteId;
        incident.IsWorkRelated = model.IsWorkRelated;
        incident.PersonsInvolved = model.PersonsInvolved;
        incident.DaysLost = model.DaysLost;
        incident.ImmediateActionTaken = model.ImmediateActionTaken;

        if (User.IsInRole(DbSeeder.AdminRole) || User.IsInRole(DbSeeder.ManagerRole))
        {
            incident.Status = model.Status;
            incident.AssignedToId = model.AssignedToId;
            if (model.Status == IncidentStatus.Closed)
            {
                incident.ClosedAt ??= DateTime.UtcNow;
            }
        }

        incident.UpdatedAt = DateTime.UtcNow;

        if (oldStatus != incident.Status && user is not null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Action = "Update",
                EntityType = "Incident",
                EntityId = incident.IncidentNumber,
                Details = $"Status changed from {oldStatus} to {incident.Status}",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Incident updated successfully.";
        return RedirectToAction(nameof(Details), new { id = incident.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ChangeStatus(int id, IncidentStatus status, string? rootCause, string? investigationNotes)
    {
        var incident = await _context.Incidents.FirstOrDefaultAsync(i => i.Id == id);
        if (incident is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        var oldStatus = incident.Status;
        incident.Status = status;
        incident.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(rootCause))
        {
            incident.RootCause = rootCause.Trim();
        }

        if (!string.IsNullOrWhiteSpace(investigationNotes))
        {
            incident.InvestigationNotes = investigationNotes.Trim();
        }

        if (status == IncidentStatus.Closed)
        {
            incident.ClosedAt = DateTime.UtcNow;
        }

        if (user is not null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Action = "Update",
                EntityType = "Incident",
                EntityId = incident.IncidentNumber,
                Details = $"Status changed from {oldStatus} to {status}",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Incident {incident.IncidentNumber} moved to {status}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Assign(int id, string assignedToId)
    {
        var incident = await _context.Incidents.FirstOrDefaultAsync(i => i.Id == id);
        if (incident is null)
        {
            return NotFound();
        }

        var assignee = await _userManager.FindByIdAsync(assignedToId ?? string.Empty);
        if (assignee is null)
        {
            TempData["Error"] = "Please select a valid investigator.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var user = await _userManager.GetUserAsync(User);
        incident.AssignedToId = assignee.Id;
        incident.UpdatedAt = DateTime.UtcNow;

        if (user is not null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Action = "Assign",
                EntityType = "Incident",
                EntityId = incident.IncidentNumber,
                Details = $"Investigation assigned to {assignee.FullName}",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Investigation assigned to {assignee.FullName}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private static IEnumerable<SelectListItem> TypeOptions(IncidentType? selected = null)
        => Enum.GetValues<IncidentType>().Select(v => new SelectListItem(v.ToString(), v.ToString(), v == selected));

    private static IEnumerable<SelectListItem> SeverityOptions(IncidentSeverity? selected = null)
        => Enum.GetValues<IncidentSeverity>().Select(v => new SelectListItem(v.ToString(), v.ToString(), v == selected));

    private static IEnumerable<SelectListItem> StatusOptions(IncidentStatus? selected = null)
        => Enum.GetValues<IncidentStatus>().Select(v => new SelectListItem(v.ToString(), v.ToString(), v == selected));

    private async Task<IEnumerable<SelectListItem>> SiteOptionsAsync(int? selectedId = null)
    {
        var sites = await _context.Sites.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
        return sites.Select(s => new SelectListItem($"{s.Code} - {s.Name}", s.Id.ToString(), s.Id == selectedId));
    }

    private async Task<IEnumerable<SelectListItem>> UserOptionsAsync(string? selectedId = null)
    {
        var users = await _context.Users.AsNoTracking().OrderBy(u => u.FullName).ToListAsync();
        return users.Select(u => new SelectListItem($"{u.FullName} ({u.Email})", u.Id, u.Id == selectedId));
    }
}
