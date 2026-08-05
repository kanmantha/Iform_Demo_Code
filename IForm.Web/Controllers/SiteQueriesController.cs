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
public class SiteQueriesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public SiteQueriesController(ApplicationDbContext context, UserManager<AppUser> userManager, IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
    }

    public async Task<IActionResult> Index(
        string? search = null,
        string? project = null,
        QueryCategory? category = null,
        QueryStatus? status = null,
        bool escalated = false,
        int page = 1)
    {
        const int pageSize = 10;

        var query = _context.SiteQueries
            .AsNoTracking()
            .Include(q => q.RaisedBy)
            .Include(q => q.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(q =>
                q.QueryNumber.Contains(search) ||
                q.IpoNumber.Contains(search) ||
                q.Project.Contains(search) ||
                q.Description.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(project)) query = query.Where(q => q.Project == project);
        if (category.HasValue) query = query.Where(q => q.Category == category.Value);
        if (status.HasValue) query = query.Where(q => q.Status == status.Value);
        if (escalated)
        {
            query = query.Where(q => q.Status != QueryStatus.Resolved
                && q.CreatedAt <= DateTime.UtcNow.AddDays(-UiClasses.EscalationThresholdDays));
        }

        query = query.OrderByDescending(q => q.CreatedAt);

        page = Math.Max(1, page);
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var siteQueries = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var model = new SiteQueryListViewModel
        {
            Search = search,
            Project = project,
            Category = category,
            Status = status,
            Escalated = escalated,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            SiteQueries = siteQueries,
            CategoryOptions = CategoryOptions(category),
            StatusOptions = StatusOptions(status),
            ProjectOptions = await ProjectOptionsAsync()
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var siteQuery = await _context.SiteQueries
            .AsNoTracking()
            .Include(q => q.RaisedBy)
            .Include(q => q.ResolvedBy)
            .Include(q => q.Product)
            .Include(q => q.Photos)
            .Include(q => q.Comments)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (siteQuery is null)
        {
            return NotFound();
        }

        var model = new SiteQueryDetailViewModel
        {
            SiteQuery = siteQuery,
            CanResolve = User.IsInRole(DbSeeder.AdminRole) || User.IsInRole(DbSeeder.ManagerRole)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new SiteQueryFormViewModel
        {
            CategoryOptions = CategoryOptions(),
            ProductOptions = await ProductOptionsAsync(),
            ProjectOptions = await ProjectOptionsAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SiteQueryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.CategoryOptions = CategoryOptions();
            model.ProductOptions = await ProductOptionsAsync();
            model.ProjectOptions = await ProjectOptionsAsync();
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var nextNumber = (await _context.SiteQueries.MaxAsync(q => (int?)q.Id)) + 1 ?? 1;

        var product = model.ProductId.HasValue
            ? await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == model.ProductId)
            : null;

        var siteQuery = new SiteQuery
        {
            QueryNumber = $"QY-{nextNumber:D3}",
            IpoNumber = model.IpoNumber.Trim(),
            Project = model.Project.Trim(),
            Category = model.Category,
            Status = QueryStatus.Pending,
            Description = model.Description.Trim(),
            QuantityNos = model.QuantityNos,
            QuantitySqm = model.QuantitySqm,
            ProductId = product?.Id,
            ProductCode = product?.ProductCode,
            SlabTargetDate = model.SlabTargetDate.HasValue
                ? DateTime.SpecifyKind(model.SlabTargetDate.Value, DateTimeKind.Local).ToUniversalTime()
                : null,
            SlabCompletedDate = model.SlabCompletedDate.HasValue
                ? DateTime.SpecifyKind(model.SlabCompletedDate.Value, DateTimeKind.Local).ToUniversalTime()
                : null,
            RaisedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        if (model.Photo is not null && model.Photo.Length > 0)
        {
            if (TryGetPhotoError(model.Photo, out var photoError))
            {
                ModelState.AddModelError(nameof(model.Photo), photoError);
            }
            else
            {
                var photoPath = await SavePhotoAsync(model.Photo);
                siteQuery.PhotoPath = photoPath;
                _context.SiteQueryPhotos.Add(new SiteQueryPhoto
                {
                    SiteQuery = siteQuery,
                    FilePath = photoPath,
                    UploadedAt = DateTime.UtcNow
                });
            }
        }

        if (!ModelState.IsValid)
        {
            model.CategoryOptions = CategoryOptions();
            model.ProductOptions = await ProductOptionsAsync();
            model.ProjectOptions = await ProjectOptionsAsync();
            return View(model);
        }

        _context.SiteQueries.Add(siteQuery);
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            UserName = user.FullName,
            Action = "Create",
            EntityType = "SiteQuery",
            EntityId = siteQuery.QueryNumber,
            Details = $"Query {siteQuery.QueryNumber} raised for {siteQuery.Project} (IPO {siteQuery.IpoNumber})",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Query {siteQuery.QueryNumber} raised for {siteQuery.Project}.";
        return RedirectToAction(nameof(Details), new { id = siteQuery.Id });
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(int id)
    {
        var siteQuery = await _context.SiteQueries
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == id);

        if (siteQuery is null)
        {
            return NotFound();
        }

        var model = new SiteQueryFormViewModel
        {
            Id = siteQuery.Id,
            IpoNumber = siteQuery.IpoNumber,
            Project = siteQuery.Project,
            Category = siteQuery.Category,
            QuantityNos = siteQuery.QuantityNos,
            QuantitySqm = siteQuery.QuantitySqm,
            Description = siteQuery.Description,
            ProductId = siteQuery.ProductId,
            SlabTargetDate = siteQuery.SlabTargetDate?.ToLocalTime(),
            SlabCompletedDate = siteQuery.SlabCompletedDate?.ToLocalTime(),
            CategoryOptions = CategoryOptions(),
            ProductOptions = await ProductOptionsAsync(siteQuery.ProductId),
            ProjectOptions = await ProjectOptionsAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(SiteQueryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.CategoryOptions = CategoryOptions();
            model.ProductOptions = await ProductOptionsAsync(model.ProductId);
            model.ProjectOptions = await ProjectOptionsAsync();
            return View(model);
        }

        var siteQuery = await _context.SiteQueries.FirstOrDefaultAsync(q => q.Id == model.Id);
        if (siteQuery is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        var product = model.ProductId.HasValue
            ? await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == model.ProductId)
            : null;

        siteQuery.IpoNumber = model.IpoNumber.Trim();
        siteQuery.Project = model.Project.Trim();
        siteQuery.Category = model.Category;
        siteQuery.Description = model.Description.Trim();
        siteQuery.QuantityNos = model.QuantityNos;
        siteQuery.QuantitySqm = model.QuantitySqm;
        siteQuery.ProductId = product?.Id;
        siteQuery.ProductCode = product?.ProductCode;
        siteQuery.SlabTargetDate = ToUtcOrNull(model.SlabTargetDate);
        siteQuery.SlabCompletedDate = ToUtcOrNull(model.SlabCompletedDate);
        siteQuery.SlabDelayDays = siteQuery.SlabCompletedDate.HasValue
            ? Math.Max(0, (int)(siteQuery.SlabCompletedDate.Value - siteQuery.CreatedAt).TotalDays)
            : null;
        siteQuery.UpdatedAt = DateTime.UtcNow;

        if (model.Photo is not null && model.Photo.Length > 0)
        {
            if (TryGetPhotoError(model.Photo, out var photoError))
            {
                ModelState.AddModelError(nameof(model.Photo), photoError);
            }
            else
            {
                var photoPath = await SavePhotoAsync(model.Photo);
                siteQuery.PhotoPath ??= photoPath;
                _context.SiteQueryPhotos.Add(new SiteQueryPhoto
                {
                    SiteQueryId = siteQuery.Id,
                    FilePath = photoPath,
                    UploadedAt = DateTime.UtcNow
                });
            }
        }

        if (!ModelState.IsValid)
        {
            model.CategoryOptions = CategoryOptions();
            model.ProductOptions = await ProductOptionsAsync(model.ProductId);
            model.ProjectOptions = await ProjectOptionsAsync();
            return View(model);
        }

        if (user is not null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Action = "Update",
                EntityType = "SiteQuery",
                EntityId = siteQuery.QueryNumber,
                Details = $"Query {siteQuery.QueryNumber} details updated",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Query {siteQuery.QueryNumber} updated.";
        return RedirectToAction(nameof(Details), new { id = siteQuery.Id });
    }

    private static DateTime? ToUtcOrNull(DateTime? value)
        => value.HasValue
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Local).ToUniversalTime()
            : null;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int id, string newComment)
    {
        var siteQuery = await _context.SiteQueries.FirstOrDefaultAsync(q => q.Id == id);
        if (siteQuery is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (siteQuery.Status == QueryStatus.Resolved)
        {
            TempData["Error"] = "Comments can only be added to open queries.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!string.IsNullOrWhiteSpace(newComment))
        {
            _context.QueryComments.Add(new QueryComment
            {
                SiteQueryId = siteQuery.Id,
                Body = newComment.Trim(),
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            });
            siteQuery.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Comment added.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ChangeStatus(int id, QueryStatus status, string? resolutionNotes)
    {
        var siteQuery = await _context.SiteQueries.FirstOrDefaultAsync(q => q.Id == id);
        if (siteQuery is null)
        {
            return NotFound();
        }

        if (status == QueryStatus.Pending)
        {
            TempData["Error"] = "A query cannot be moved back to Pending.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var user = await _userManager.GetUserAsync(User);
        var oldStatus = siteQuery.Status;
        siteQuery.Status = status;
        siteQuery.UpdatedAt = DateTime.UtcNow;

        if (status == QueryStatus.Resolved)
        {
            siteQuery.ResolvedById = user?.Id;
            siteQuery.ResolvedAt = DateTime.UtcNow;

            if (user is not null && siteQuery.RaisedById != user.Id)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = siteQuery.RaisedById,
                    Title = $"Query {siteQuery.QueryNumber} resolved",
                    Body = $"{siteQuery.Project} (IPO {siteQuery.IpoNumber}) was marked Resolved by {user.FullName}.",
                    SiteQueryId = siteQuery.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        else if (siteQuery.ResolvedAt.HasValue)
        {
            siteQuery.ResolvedAt = null;
            siteQuery.ResolvedById = null;
        }

        if (!string.IsNullOrWhiteSpace(resolutionNotes) && user is not null)
        {
            _context.QueryComments.Add(new QueryComment
            {
                SiteQueryId = siteQuery.Id,
                Body = resolutionNotes.Trim(),
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (user is not null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Action = "Update",
                EntityType = "SiteQuery",
                EntityId = siteQuery.QueryNumber,
                Details = $"Dispatch status changed from {oldStatus} to {status}",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Query {siteQuery.QueryNumber} moved to {status}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Pdf(int id)
    {
        var siteQuery = await _context.SiteQueries
            .AsNoTracking()
            .Include(q => q.RaisedBy)
            .Include(q => q.ResolvedBy)
            .Include(q => q.Product)
            .Include(q => q.Photos)
            .Include(q => q.Comments)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (siteQuery is null)
        {
            return NotFound();
        }

        var bytes = Services.SiteQueryPdfService.BuildReport(siteQuery, _environment.WebRootPath);
        return File(bytes, "application/pdf", $"IFORM-{siteQuery.QueryNumber}.pdf");
    }

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Dashboard()    {
        var queries = await _context.SiteQueries
            .AsNoTracking()
            .Include(q => q.RaisedBy)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var open = queries.Where(q => q.Status != QueryStatus.Resolved).ToList();
        var resolved = queries.Where(q => q.Status == QueryStatus.Resolved).ToList();
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var projectDelays = open
            .GroupBy(q => q.Project)
            .Select(g => new ProjectDelay(
                g.Key,
                g.Count(),
                g.Max(q => q.DelayDays),
                g.GroupBy(q => q.Category).OrderByDescending(c => c.Count()).Select(c => c.Key.ToString()).FirstOrDefault()))
            .OrderByDescending(p => p.MaxDelayDays)
            .ToList();

        var raisedByStats = open
            .GroupBy(q => q.RaisedBy?.FullName ?? "Unknown")
            .Select(g => new RaisedByStat(g.Key, g.Count(), g.Max(q => q.DelayDays)))
            .OrderByDescending(r => r.MaxDelayDays)
            .Take(5)
            .ToList();

        var model = new SiteQueryDashboardViewModel
        {
            TotalQueries = queries.Count,
            OpenQueries = open.Count,
            ResolvedQueries = resolved.Count,
            ResolvedThisMonth = resolved.Count(q => q.ResolvedAt >= monthStart),
            EscalatedQueries = open.Count(q => UiClasses.IsEscalated(q)),
            AvgDelayDays = queries.Count > 0 ? queries.Average(q => q.DelayDays) : 0,
            ProjectDelays = projectDelays,
            ByCategory = open.GroupBy(q => q.Category).ToDictionary(g => g.Key.ToString(), g => g.Count()),
            OldestOpen = open.OrderByDescending(q => q.DelayDays).Take(5).ToList(),
            RaisedByStats = raisedByStats
        };

        return View(model);
    }

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Email(int id)
    {
        var siteQuery = await _context.SiteQueries
            .AsNoTracking()
            .Include(q => q.RaisedBy)
            .Include(q => q.Product)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (siteQuery is null)
        {
            return NotFound();
        }

        var (subject, body) = BuildEmail(siteQuery);
        ViewData["EmailSubject"] = subject;
        ViewData["EmailBody"] = body;
        return View(siteQuery);
    }

    private static (string Subject, string Body) BuildEmail(SiteQuery siteQuery)
    {
        var categoryText = CategoryText(siteQuery.Category);
        var issueLine = siteQuery.Category switch
        {
            QueryCategory.Missing => "The item listed above is reported as missing and is pending dispatch.",
            QueryCategory.ProductionMistake => "The item listed above has a production mistake that needs review and re-dispatch.",
            QueryCategory.DesignMistake => "The item listed above has a design mistake that requires design review before dispatch.",
            QueryCategory.DispatchMissing => "The dispatch for the item listed above is missing and has not reached the site.",
            _ => "Please review the issue listed above."
        };

        var subject = $"[IPO {siteQuery.IpoNumber}] {siteQuery.Project} \u2013 {categoryText} Reported";

        var body =
            $"Dear Manager,\n\n" +
            $"A site query has been raised for Project {siteQuery.Project} (IPO {siteQuery.IpoNumber}). " +
            $"Issue type: {categoryText}.\n\n" +
            $"{issueLine}\n\n" +
            $"Quantity: {siteQuery.QuantityNos} nos / {siteQuery.QuantitySqm} sqm\n" +
            (siteQuery.ProductCode is not null ? $"Verified product: {siteQuery.ProductCode}\n" : "") +
            $"Slab target casting date: {(siteQuery.SlabTargetDate?.ToLocalTime().ToString("dd/MM/yyyy") ?? "Not set")}\n" +
            $"Current delay: {siteQuery.DelayDays} days\n\n" +
            $"Raised by: {siteQuery.RaisedBy?.FullName ?? "Site Engineer"}\n" +
            $"Date: {siteQuery.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}\n\n" +
            $"Please review and advise on dispatch status.";

        return (subject, body);
    }

    private static string CategoryText(QueryCategory category) => category switch
    {
        QueryCategory.Missing => "Missing",
        QueryCategory.ProductionMistake => "Production Mistake",
        QueryCategory.DesignMistake => "Design Mistake",
        QueryCategory.DispatchMissing => "Dispatch Missing",
        _ => category.ToString()
    };

    private static IEnumerable<SelectListItem> CategoryOptions(QueryCategory? selected = null)
        => Enum.GetValues<QueryCategory>().Select(v =>
            new SelectListItem(CategoryText(v), v.ToString(), v == selected));

    private static IEnumerable<SelectListItem> StatusOptions(QueryStatus? selected = null)
        => Enum.GetValues<QueryStatus>().Select(v => new SelectListItem(v.ToString(), v.ToString(), v == selected));

    private async Task<IReadOnlyList<string>> ProjectOptionsAsync()
        => await _context.SiteQueries
            .AsNoTracking()
            .Where(q => !string.IsNullOrWhiteSpace(q.Project))
            .Select(q => q.Project)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();

    private async Task<IEnumerable<SelectListItem>> ProductOptionsAsync(int? selectedId = null)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Family)
            .ThenBy(p => p.ProductCode)
            .ToListAsync();

        var groups = new Dictionary<string, SelectListGroup>(StringComparer.Ordinal);
        return products.Select(p =>
        {
            var family = string.IsNullOrWhiteSpace(p.Family) ? "Other" : p.Family;
            if (!groups.TryGetValue(family, out var group))
            {
                group = new SelectListGroup { Name = family };
                groups[family] = group;
            }

            return new SelectListItem
            {
                Text = $"{p.ProductCode} - {p.Name}",
                Value = p.Id.ToString(),
                Selected = p.Id == selectedId,
                Group = group
            };
        });
    }

    private static readonly string[] AllowedPhotoExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const int MaxPhotoBytes = 5 * 1024 * 1024;

    private static bool TryGetPhotoError(IFormFile file, out string error)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedPhotoExtensions.Contains(extension))
        {
            error = $"Unsupported file type \"{extension}\". Allowed: {string.Join(", ", AllowedPhotoExtensions)}.";
            return true;
        }

        if (file.Length > MaxPhotoBytes)
        {
            error = "Photo exceeds the 5 MB limit.";
            return true;
        }

        error = string.Empty;
        return false;
    }

    private async Task<string> SavePhotoAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedPhotoExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                $"Unsupported file type \"{extension}\". Allowed: {string.Join(", ", AllowedPhotoExtensions)}.");
        }

        if (file.Length > MaxPhotoBytes)
        {
            throw new InvalidOperationException("Photo exceeds the 5 MB limit.");
        }

        var folder = Path.Combine(_environment.WebRootPath, "uploads", "queries");
        Directory.CreateDirectory(folder);

        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".jpg";
        }

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folder, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/queries/{fileName}";
    }
}
