using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.Services;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class PerformanceController : Controller
{
    private readonly ApplicationDbContext _context;

    public PerformanceController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(PerformanceStatus? status, PerformanceRating? rating, string? search)
    {
        var query = _context.PerformanceReviews.Include(p => p.Employee).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.ReviewPeriod.Contains(term) || (p.Employee != null && (p.Employee.FirstName.Contains(term) || p.Employee.LastName.Contains(term))));
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (rating.HasValue)
        {
            query = query.Where(p => p.Rating == rating.Value);
        }

        var reviews = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

        var model = new PerformanceListViewModel
        {
            Reviews = reviews,
            Status = status,
            Rating = rating,
            Search = search,
            TotalCount = await _context.PerformanceReviews.CountAsync()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new PerformanceFormViewModel { StartDate = DateTime.UtcNow };
        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PerformanceFormViewModel model)
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

        var review = new PerformanceReview
        {
            EmployeeId = model.EmployeeId,
            ReviewPeriod = model.ReviewPeriod.Trim(),
            Rating = model.Rating,
            Status = model.Status,
            Goals = model.Goals,
            Feedback = model.Feedback,
            Improvements = model.Improvements,
            StartDate = model.StartDate == default ? DateTime.UtcNow.Date : UtcDates.Date(model.StartDate),
            EndDate = model.EndDate.HasValue ? UtcDates.Date(model.EndDate.Value) : null,
            CreatedAt = DateTime.UtcNow
        };

        if (review.Status == PerformanceStatus.Completed || review.Status == PerformanceStatus.Approved)
        {
            review.ReviewedAt = DateTime.UtcNow;
            review.ReviewedById = User.Identity?.Name;
        }

        _context.PerformanceReviews.Add(review);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Performance review created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var review = await _context.PerformanceReviews.Include(p => p.Employee).AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (review is null)
        {
            return NotFound();
        }
        return View(review);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var review = await _context.PerformanceReviews.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (review is null)
        {
            return NotFound();
        }

        var model = new PerformanceFormViewModel
        {
            Id = review.Id,
            EmployeeId = review.EmployeeId,
            ReviewPeriod = review.ReviewPeriod,
            Rating = review.Rating,
            Status = review.Status,
            Goals = review.Goals,
            Feedback = review.Feedback,
            Improvements = review.Improvements,
            StartDate = review.StartDate.ToLocalTime(),
            EndDate = review.EndDate?.ToLocalTime()
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PerformanceFormViewModel model)
    {
        await PopulateOptionsAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var review = await _context.PerformanceReviews.FirstOrDefaultAsync(p => p.Id == model.Id);
        if (review is null)
        {
            return NotFound();
        }

        if (!await _context.Employees.AnyAsync(e => e.Id == model.EmployeeId))
        {
            ModelState.AddModelError(nameof(model.EmployeeId), "Select a valid employee.");
            return View(model);
        }

        review.EmployeeId = model.EmployeeId;
        review.ReviewPeriod = model.ReviewPeriod.Trim();
        review.Rating = model.Rating;
        review.Status = model.Status;
        review.Goals = model.Goals;
        review.Feedback = model.Feedback;
        review.Improvements = model.Improvements;
        review.StartDate = model.StartDate == default ? DateTime.UtcNow.Date : UtcDates.Date(model.StartDate);
        review.EndDate = model.EndDate.HasValue ? UtcDates.Date(model.EndDate.Value) : null;

        if ((review.Status == PerformanceStatus.Completed || review.Status == PerformanceStatus.Approved) && review.ReviewedAt == null)
        {
            review.ReviewedAt = DateTime.UtcNow;
            review.ReviewedById = User.Identity?.Name;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Performance review updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await _context.PerformanceReviews.FirstOrDefaultAsync(p => p.Id == id);
        if (review is null)
        {
            return NotFound();
        }

        _context.PerformanceReviews.Remove(review);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Performance review deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(PerformanceFormViewModel model)
    {
        model.Employees = await _context.Employees
            .AsNoTracking()
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .ToListAsync();
    }
}
