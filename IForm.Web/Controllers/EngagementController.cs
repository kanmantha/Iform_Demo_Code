using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.Services;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class EngagementController : Controller
{
    private readonly ApplicationDbContext _context;

    public EngagementController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(EngagementStatus? status, string? search)
    {
        var query = _context.EngagementSurveys.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s => s.Title.Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        var surveys = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();

        var model = new EngagementListViewModel
        {
            Surveys = surveys,
            Status = status,
            Search = search,
            TotalCount = await _context.EngagementSurveys.CountAsync()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new EngagementFormViewModel { StartDate = DateTime.UtcNow };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EngagementFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var survey = new EngagementSurvey
        {
            Title = model.Title.Trim(),
            Description = model.Description,
            Status = model.Status,
            StartDate = UtcDates.Date(model.StartDate),
            EndDate = model.EndDate.HasValue ? UtcDates.Date(model.EndDate.Value) : null,
            CreatedAt = DateTime.UtcNow
        };

        _context.EngagementSurveys.Add(survey);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Survey created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var survey = await _context.EngagementSurveys.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (survey is null)
        {
            return NotFound();
        }
        return View(survey);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var survey = await _context.EngagementSurveys.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (survey is null)
        {
            return NotFound();
        }

        var model = new EngagementFormViewModel
        {
            Id = survey.Id,
            Title = survey.Title,
            Description = survey.Description,
            Status = survey.Status,
            StartDate = survey.StartDate.ToLocalTime(),
            EndDate = survey.EndDate?.ToLocalTime()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EngagementFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var survey = await _context.EngagementSurveys.FirstOrDefaultAsync(s => s.Id == model.Id);
        if (survey is null)
        {
            return NotFound();
        }

        survey.Title = model.Title.Trim();
        survey.Description = model.Description;
        survey.Status = model.Status;
        survey.StartDate = UtcDates.Date(model.StartDate);
        survey.EndDate = model.EndDate.HasValue ? UtcDates.Date(model.EndDate.Value) : null;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Survey updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var survey = await _context.EngagementSurveys.FirstOrDefaultAsync(s => s.Id == id);
        if (survey is null)
        {
            return NotFound();
        }

        _context.EngagementSurveys.Remove(survey);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Survey deleted.";
        return RedirectToAction(nameof(Index));
    }
}
