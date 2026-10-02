using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.Services;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class ProjectsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProjectsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(ProjectStatus? status, string? search)
    {
        var query = _context.Projects.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Name.Contains(term) || (p.Description != null && p.Description.Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        var projects = await query.OrderBy(p => p.Name).ToListAsync();

        var model = new ProjectListViewModel
        {
            Projects = projects,
            Status = status,
            Search = search,
            TotalCount = await _context.Projects.CountAsync(),
            PlannedCount = await _context.Projects.CountAsync(p => p.Status == ProjectStatus.Planned),
            ActiveCount = await _context.Projects.CountAsync(p => p.Status == ProjectStatus.Active),
            OnHoldCount = await _context.Projects.CountAsync(p => p.Status == ProjectStatus.OnHold),
            CompletedCount = await _context.Projects.CountAsync(p => p.Status == ProjectStatus.Completed),
            CancelledCount = await _context.Projects.CountAsync(p => p.Status == ProjectStatus.Cancelled)
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new ProjectFormViewModel();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var project = new Project
        {
            Name = model.Name.Trim(),
            Description = model.Description,
            Status = model.Status,
            StartDate = UtcDates.Date(model.StartDate),
            EndDate = UtcDates.Date(model.EndDate),
            CreatedAt = DateTime.UtcNow
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Project created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var project = await _context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (project is null)
        {
            return NotFound();
        }
        return View(project);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var project = await _context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (project is null)
        {
            return NotFound();
        }

        var model = new ProjectFormViewModel
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            Status = project.Status,
            StartDate = project.StartDate?.ToLocalTime(),
            EndDate = project.EndDate?.ToLocalTime()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProjectFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == model.Id);
        if (project is null)
        {
            return NotFound();
        }

        project.Name = model.Name.Trim();
        project.Description = model.Description;
        project.Status = model.Status;
        project.StartDate = UtcDates.Date(model.StartDate);
        project.EndDate = UtcDates.Date(model.EndDate);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Project updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null)
        {
            return NotFound();
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Project deleted.";
        return RedirectToAction(nameof(Index));
    }
}
