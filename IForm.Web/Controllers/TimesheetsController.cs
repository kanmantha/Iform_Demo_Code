using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.Services;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class TimesheetsController : Controller
{
    private readonly ApplicationDbContext _context;

    public TimesheetsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? employeeId, int? projectId, TimesheetStatus? status, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.Timesheets
            .Include(t => t.Employee)
            .Include(t => t.Project)
            .AsNoTracking();

        if (employeeId.HasValue)
        {
            query = query.Where(t => t.EmployeeId == employeeId.Value);
        }

        if (projectId.HasValue)
        {
            query = query.Where(t => t.ProjectId == projectId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            var from = UtcDates.Date(fromDate.Value);
            query = query.Where(t => t.Date >= from);
        }

        if (toDate.HasValue)
        {
            var to = UtcDates.Date(toDate.Value);
            query = query.Where(t => t.Date <= to);
        }

        var timesheets = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .ToListAsync();

        var model = new TimesheetListViewModel
        {
            Timesheets = timesheets,
            Employees = await _context.Employees.AsNoTracking().OrderBy(e => e.FirstName).ThenBy(e => e.LastName).ToListAsync(),
            Projects = await _context.Projects.AsNoTracking().OrderBy(p => p.Name).ToListAsync(),
            EmployeeId = employeeId,
            ProjectId = projectId,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate,
            TotalCount = await _context.Timesheets.CountAsync(),
            DraftCount = await _context.Timesheets.CountAsync(t => t.Status == TimesheetStatus.Draft),
            SubmittedCount = await _context.Timesheets.CountAsync(t => t.Status == TimesheetStatus.Submitted),
            ApprovedCount = await _context.Timesheets.CountAsync(t => t.Status == TimesheetStatus.Approved),
            RejectedCount = await _context.Timesheets.CountAsync(t => t.Status == TimesheetStatus.Rejected)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new TimesheetFormViewModel
        {
            Date = DateTime.UtcNow.Date
        };
        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TimesheetFormViewModel model)
    {
        await PopulateOptionsAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await _context.Employees.AnyAsync(e => e.Id == model.EmployeeId))
        {
            ModelState.AddModelError(nameof(model.EmployeeId), "Select a valid employee.");
            return View(model);
        }

        if (model.ProjectId.HasValue && !await _context.Projects.AnyAsync(p => p.Id == model.ProjectId.Value))
        {
            ModelState.AddModelError(nameof(model.ProjectId), "Select a valid project.");
            return View(model);
        }

        var timesheet = new Timesheet
        {
            EmployeeId = model.EmployeeId,
            ProjectId = model.ProjectId,
            Date = UtcDates.Date(model.Date),
            Hours = model.Hours,
            Notes = model.Notes,
            Status = model.Status
        };

        if (timesheet.Status == TimesheetStatus.Submitted && timesheet.SubmittedAt == null)
        {
            timesheet.SubmittedAt = DateTime.UtcNow;
        }
        if (timesheet.Status == TimesheetStatus.Approved && timesheet.ApprovedAt == null)
        {
            timesheet.ApprovedAt = DateTime.UtcNow;
        }

        _context.Timesheets.Add(timesheet);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Timesheet created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var timesheet = await _context.Timesheets
            .Include(t => t.Employee)
            .Include(t => t.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (timesheet is null)
        {
            return NotFound();
        }

        return View(timesheet);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var timesheet = await _context.Timesheets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (timesheet is null)
        {
            return NotFound();
        }

        var model = new TimesheetFormViewModel
        {
            Id = timesheet.Id,
            EmployeeId = timesheet.EmployeeId,
            ProjectId = timesheet.ProjectId,
            Date = timesheet.Date.ToLocalTime(),
            Hours = timesheet.Hours,
            Notes = timesheet.Notes,
            Status = timesheet.Status
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TimesheetFormViewModel model)
    {
        await PopulateOptionsAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var timesheet = await _context.Timesheets.FirstOrDefaultAsync(t => t.Id == model.Id);
        if (timesheet is null)
        {
            return NotFound();
        }

        if (!await _context.Employees.AnyAsync(e => e.Id == model.EmployeeId))
        {
            ModelState.AddModelError(nameof(model.EmployeeId), "Select a valid employee.");
            return View(model);
        }

        if (model.ProjectId.HasValue && !await _context.Projects.AnyAsync(p => p.Id == model.ProjectId.Value))
        {
            ModelState.AddModelError(nameof(model.ProjectId), "Select a valid project.");
            return View(model);
        }

        timesheet.EmployeeId = model.EmployeeId;
        timesheet.ProjectId = model.ProjectId;
        timesheet.Date = UtcDates.Date(model.Date);
        timesheet.Hours = model.Hours;
        timesheet.Notes = model.Notes;
        timesheet.Status = model.Status;

        if (timesheet.Status == TimesheetStatus.Submitted && timesheet.SubmittedAt == null)
        {
            timesheet.SubmittedAt = DateTime.UtcNow;
        }
        if (timesheet.Status == TimesheetStatus.Approved && timesheet.ApprovedAt == null)
        {
            timesheet.ApprovedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Timesheet updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var timesheet = await _context.Timesheets.FirstOrDefaultAsync(t => t.Id == id);
        if (timesheet is null)
        {
            return NotFound();
        }

        _context.Timesheets.Remove(timesheet);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Timesheet deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(TimesheetFormViewModel model)
    {
        model.Employees = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Status != EmploymentStatus.Exited)
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .ToListAsync();

        model.Projects = await _context.Projects
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}
