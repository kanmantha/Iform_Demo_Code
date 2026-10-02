using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class LmsController : Controller
{
    private readonly ApplicationDbContext _context;

    public LmsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(LmsStatus? status, string? search)
    {
        var query = _context.LmsCourses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.Title.Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        var courses = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

        var model = new LmsListViewModel
        {
            Courses = courses,
            Status = status,
            Search = search,
            TotalCount = await _context.LmsCourses.CountAsync()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new LmsFormViewModel();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LmsFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var course = new LmsCourse
        {
            Title = model.Title.Trim(),
            Description = model.Description,
            Status = model.Status,
            DurationHours = model.DurationHours,
            CreatedAt = DateTime.UtcNow
        };

        _context.LmsCourses.Add(course);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Course created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var course = await _context.LmsCourses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (course is null)
        {
            return NotFound();
        }
        return View(course);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var course = await _context.LmsCourses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (course is null)
        {
            return NotFound();
        }

        var model = new LmsFormViewModel
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            Status = course.Status,
            DurationHours = course.DurationHours
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(LmsFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var course = await _context.LmsCourses.FirstOrDefaultAsync(c => c.Id == model.Id);
        if (course is null)
        {
            return NotFound();
        }

        course.Title = model.Title.Trim();
        course.Description = model.Description;
        course.Status = model.Status;
        course.DurationHours = model.DurationHours;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Course updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var course = await _context.LmsCourses.FirstOrDefaultAsync(c => c.Id == id);
        if (course is null)
        {
            return NotFound();
        }

        _context.LmsCourses.Remove(course);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Course deleted.";
        return RedirectToAction(nameof(Index));
    }
}
