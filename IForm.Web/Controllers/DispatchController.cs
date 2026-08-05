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
public class DispatchController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public DispatchController(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(
        string? search = null,
        DispatchStatus? status = null,
        QueryCategory? reason = null,
        int page = 1)
    {
        const int pageSize = 10;

        var query = _context.DispatchOrders
            .AsNoTracking()
            .Include(d => d.CreatedBy)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(d =>
                d.DispatchNumber.Contains(search) ||
                d.IpoNumber.Contains(search) ||
                d.Project.Contains(search));
        }

        if (status.HasValue) query = query.Where(d => d.DispatchStatus == status.Value);
        if (reason.HasValue) query = query.Where(d => d.Reason == reason.Value);

        var pendingCount = await _context.DispatchOrders
            .AsNoTracking()
            .CountAsync(d => d.DispatchStatus == DispatchStatus.Pending);

        query = query.OrderByDescending(d => d.CreatedAt);

        page = Math.Max(1, page);
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var dispatchOrders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var model = new DispatchListViewModel
        {
            Search = search,
            Status = status,
            Reason = reason,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            PendingCount = pendingCount,
            DispatchOrders = dispatchOrders,
            StatusOptions = StatusOptions(status),
            ReasonOptions = ReasonOptions(reason)
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.DispatchOrders
            .AsNoTracking()
            .Include(d => d.CreatedBy)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        var model = new DispatchDetailViewModel
        {
            DispatchOrder = order,
            CanEdit = User.IsInRole(DbSeeder.AdminRole) || User.IsInRole(DbSeeder.ManagerRole)
        };

        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Create()
    {
        var model = new DispatchFormViewModel
        {
            StatusOptions = StatusOptions(),
            ReasonOptions = ReasonOptions()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create(DispatchFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.StatusOptions = StatusOptions();
            model.ReasonOptions = ReasonOptions();
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var nextNumber = (await _context.DispatchOrders.MaxAsync(d => (int?)d.Id)) + 1 ?? 1;

        var order = new DispatchOrder
        {
            DispatchNumber = $"DT-{nextNumber:D2}",
            IpoNumber = model.IpoNumber.Trim(),
            Project = model.Project.Trim(),
            SlabTargetDate = ToUtcOrNull(model.SlabTargetDate),
            SlabCompletedDate = ToUtcOrNull(model.SlabCompletedDate),
            SlabDelayDays = model.SlabDelayDays,
            QuantityNos = model.QuantityNos,
            QuantitySqm = model.QuantitySqm,
            CastingDate = ToUtcOrNull(model.CastingDate),
            DispatchStatus = model.DispatchStatus,
            DispatchDate = ToUtcOrNull(model.DispatchDate),
            DelayDays = model.DelayDays,
            Reason = model.Reason,
            Remarks = model.Remarks?.Trim(),
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.DispatchOrders.Add(order);
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            UserName = user.FullName,
            Action = "Create",
            EntityType = "DispatchOrder",
            EntityId = order.DispatchNumber,
            Details = $"Dispatch entry {order.DispatchNumber} created for {order.Project} (IPO {order.IpoNumber})",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Dispatch entry {order.DispatchNumber} created for {order.Project}.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(int id)
    {
        var order = await _context.DispatchOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        var model = new DispatchFormViewModel
        {
            Id = order.Id,
            DispatchNumber = order.DispatchNumber,
            IpoNumber = order.IpoNumber,
            Project = order.Project,
            SlabTargetDate = order.SlabTargetDate?.ToLocalTime(),
            SlabCompletedDate = order.SlabCompletedDate?.ToLocalTime(),
            SlabDelayDays = order.SlabDelayDays,
            QuantityNos = order.QuantityNos,
            QuantitySqm = order.QuantitySqm,
            CastingDate = order.CastingDate?.ToLocalTime(),
            DispatchStatus = order.DispatchStatus,
            DispatchDate = order.DispatchDate?.ToLocalTime(),
            DelayDays = order.DelayDays,
            Reason = order.Reason,
            Remarks = order.Remarks,
            StatusOptions = StatusOptions(),
            ReasonOptions = ReasonOptions()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(DispatchFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.StatusOptions = StatusOptions();
            model.ReasonOptions = ReasonOptions();
            return View(model);
        }

        var order = await _context.DispatchOrders.FirstOrDefaultAsync(d => d.Id == model.Id);
        if (order is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);

        order.IpoNumber = model.IpoNumber.Trim();
        order.Project = model.Project.Trim();
        order.SlabTargetDate = ToUtcOrNull(model.SlabTargetDate);
        order.SlabCompletedDate = ToUtcOrNull(model.SlabCompletedDate);
        order.SlabDelayDays = model.SlabDelayDays;
        order.QuantityNos = model.QuantityNos;
        order.QuantitySqm = model.QuantitySqm;
        order.CastingDate = ToUtcOrNull(model.CastingDate);
        order.DispatchStatus = model.DispatchStatus;
        order.DispatchDate = ToUtcOrNull(model.DispatchDate);
        order.DelayDays = model.DelayDays;
        order.Reason = model.Reason;
        order.Remarks = model.Remarks?.Trim();
        order.UpdatedAt = DateTime.UtcNow;

        if (user is not null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Action = "Update",
                EntityType = "DispatchOrder",
                EntityId = order.DispatchNumber,
                Details = $"Dispatch entry {order.DispatchNumber} updated",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Dispatch entry {order.DispatchNumber} updated.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    private static IEnumerable<SelectListItem> StatusOptions(DispatchStatus? selected = null)
        => Enum.GetValues<DispatchStatus>().Select(v =>
            new SelectListItem(v.ToString(), v.ToString(), v == selected));

    private static IEnumerable<SelectListItem> ReasonOptions(QueryCategory? selected = null)
        => Enum.GetValues<QueryCategory>()
            .Select(v => new SelectListItem(ReasonText(v), v.ToString(), v == selected));

    private static string ReasonText(QueryCategory reason) => reason switch
    {
        QueryCategory.Missing => "Missing",
        QueryCategory.ProductionMistake => "Production Mistake",
        QueryCategory.DesignMistake => "Design Mistake",
        QueryCategory.DispatchMissing => "Dispatch Missing",
        _ => reason.ToString()
    };

    private static DateTime? ToUtcOrNull(DateTime? value)
        => value.HasValue
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Local).ToUniversalTime()
            : null;
}
