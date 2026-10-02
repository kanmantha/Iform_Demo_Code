using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.Services;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class TrainingController : Controller
{
    private readonly ApplicationDbContext _context;

    public TrainingController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(TrainingStatus? status, TrainingType? type, string? search)
    {
        var query = _context.Trainings.Include(t => t.Trainer).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(t => t.Title.Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(t => t.Type == type.Value);
        }

        var trainings = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

        var model = new TrainingListViewModel
        {
            Trainings = trainings,
            Status = status,
            Type = type,
            Search = search,
            TotalCount = await _context.Trainings.CountAsync()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new TrainingFormViewModel();
        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TrainingFormViewModel model)
    {
        await PopulateOptionsAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.TrainerEmployeeId.HasValue && !await _context.Employees.AnyAsync(e => e.Id == model.TrainerEmployeeId.Value))
        {
            ModelState.AddModelError(nameof(model.TrainerEmployeeId), "Select a valid trainer.");
            return View(model);
        }

        var training = new Training
        {
            Title = model.Title.Trim(),
            Description = model.Description,
            Type = model.Type,
            Status = model.Status,
            StartDate = UtcDates.Date(model.StartDate),
            EndDate = UtcDates.Date(model.EndDate),
            TrainerEmployeeId = model.TrainerEmployeeId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Trainings.Add(training);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Training created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var training = await _context.Trainings.Include(t => t.Trainer).AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (training is null)
        {
            return NotFound();
        }
        return View(training);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var training = await _context.Trainings.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (training is null)
        {
            return NotFound();
        }

        var model = new TrainingFormViewModel
        {
            Id = training.Id,
            Title = training.Title,
            Description = training.Description,
            Type = training.Type,
            Status = training.Status,
            StartDate = training.StartDate?.ToLocalTime(),
            EndDate = training.EndDate?.ToLocalTime(),
            TrainerEmployeeId = training.TrainerEmployeeId
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TrainingFormViewModel model)
    {
        await PopulateOptionsAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var training = await _context.Trainings.FirstOrDefaultAsync(t => t.Id == model.Id);
        if (training is null)
        {
            return NotFound();
        }

        if (model.TrainerEmployeeId.HasValue && !await _context.Employees.AnyAsync(e => e.Id == model.TrainerEmployeeId.Value))
        {
            ModelState.AddModelError(nameof(model.TrainerEmployeeId), "Select a valid trainer.");
            return View(model);
        }

        training.Title = model.Title.Trim();
        training.Description = model.Description;
        training.Type = model.Type;
        training.Status = model.Status;
        training.StartDate = UtcDates.Date(model.StartDate);
        training.EndDate = UtcDates.Date(model.EndDate);
        training.TrainerEmployeeId = model.TrainerEmployeeId;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Training updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var training = await _context.Trainings.FirstOrDefaultAsync(t => t.Id == id);
        if (training is null)
        {
            return NotFound();
        }

        _context.Trainings.Remove(training);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Training deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(TrainingFormViewModel model)
    {
        model.Trainers = await _context.Employees
            .AsNoTracking()
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .ToListAsync();
    }
}
