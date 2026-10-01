using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class OnboardingController : Controller
{
    private readonly ApplicationDbContext _context;

    public OnboardingController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var onboardings = await _context.EmployeeOnboardings
            .AsNoTracking()
            .Include(o => o.Employee)
            .Include(o => o.OnboardingTemplate)
            .Include(o => o.Tasks)
            .OrderByDescending(o => o.StartedAt)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var openTasks = await _context.EmployeeOnboardingTasks
            .AsNoTracking()
            .Include(t => t.EmployeeOnboarding)
            .ThenInclude(o => o!.Employee)
            .Where(t => !t.IsCompleted && t.DueDate < today)
            .OrderBy(t => t.DueDate)
            .ToListAsync();

        var model = new OnboardingListViewModel
        {
            Onboardings = onboardings,
            OpenTasks = openTasks,
            Templates = await _context.OnboardingTemplates
                .AsNoTracking()
                .Include(t => t.TaskTemplates)
                .OrderBy(t => t.Name)
                .ToListAsync(),
            ActiveCount = onboardings.Count(o => o.CompletedAt is null),
            CompletedCount = onboardings.Count(o => o.CompletedAt is not null),
            OverdueTaskCount = openTasks.Count
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Start()
    {
        return View(new OnboardingStartFormViewModel
        {
            Employees = await EmployeesAsync(),
            Templates = await TemplateOptionsAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(OnboardingStartFormViewModel model)
    {
        model.Employees = await EmployeesAsync();
        model.Templates = await TemplateOptionsAsync();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var template = await _context.OnboardingTemplates
            .Include(t => t.TaskTemplates)
            .FirstOrDefaultAsync(t => t.Id == model.OnboardingTemplateId);

        if (template is null)
        {
            ModelState.AddModelError(nameof(model.OnboardingTemplateId), "Select a template.");
            return View(model);
        }

        if (await _context.EmployeeOnboardings.AnyAsync(o =>
                o.EmployeeId == model.EmployeeId && o.OnboardingTemplateId == model.OnboardingTemplateId))
        {
            ModelState.AddModelError(nameof(model.OnboardingTemplateId), "That employee already has this checklist started.");
            return View(model);
        }

        var startDate = DateTime.UtcNow.Date;
        var onboarding = new EmployeeOnboarding
        {
            EmployeeId = model.EmployeeId,
            OnboardingTemplateId = template.Id,
            StartedAt = DateTime.UtcNow
        };

        foreach (var taskTemplate in template.TaskTemplates.OrderBy(t => t.DisplayOrder))
        {
            onboarding.Tasks.Add(new EmployeeOnboardingTask
            {
                Title = taskTemplate.Title,
                DueDate = startDate.AddDays(taskTemplate.DueDayOffset),
                DisplayOrder = taskTemplate.DisplayOrder
            });
        }

        _context.EmployeeOnboardings.Add(onboarding);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Onboarding started with {onboarding.Tasks.Count} task(s).";
        return RedirectToAction(nameof(Details), new { id = onboarding.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var onboarding = await _context.EmployeeOnboardings
            .AsNoTracking()
            .Include(o => o.Employee)
            .Include(o => o.OnboardingTemplate)
            .Include(o => o.Tasks)
            .FirstOrDefaultAsync(o => o.Id == id);

        return onboarding is null
            ? NotFound()
            : View(new OnboardingDetailViewModel { Onboarding = onboarding });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTask(int id)
    {
        var task = await _context.EmployeeOnboardingTasks
            .Include(t => t.EmployeeOnboarding)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task is null)
        {
            return NotFound();
        }

        task.IsCompleted = !task.IsCompleted;
        task.CompletedAt = task.IsCompleted ? DateTime.UtcNow : null;
        task.CompletedById = task.IsCompleted ? CurrentUserId() : null;

        var onboarding = task.EmployeeOnboarding!;
        var allTasks = await _context.EmployeeOnboardingTasks
            .Where(t => t.EmployeeOnboardingId == onboarding.Id)
            .ToListAsync();

        onboarding.CompletedAt = allTasks.All(t => t.IsCompleted) ? DateTime.UtcNow : null;

        await _context.SaveChangesAsync();

        TempData["Success"] = task.IsCompleted ? $"'{task.Title}' completed." : $"'{task.Title}' reopened.";
        return RedirectToAction(nameof(Details), new { id = onboarding.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Templates()
    {
        var templates = await _context.OnboardingTemplates
            .AsNoTracking()
            .Include(t => t.TaskTemplates)
            .OrderBy(t => t.Name)
            .ToListAsync();

        return View(templates);
    }

    [HttpGet]
    public IActionResult CreateTemplate() => View(new OnboardingTemplateFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTemplate(OnboardingTemplateFormViewModel model)
    {
        model.Tasks = model.Tasks.Where(t => !string.IsNullOrWhiteSpace(t.Title)).ToList();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.Tasks.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Tasks), "Add at least one task to the checklist.");
            return View(model);
        }

        if (await _context.OnboardingTemplates.AnyAsync(t => t.Name == model.Name.Trim()))
        {
            ModelState.AddModelError(nameof(model.Name), "A template with this name already exists.");
            return View(model);
        }

        var template = new OnboardingTemplate
        {
            Name = model.Name.Trim(),
            Description = model.Description,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        var order = 1;
        foreach (var task in model.Tasks)
        {
            template.TaskTemplates.Add(new OnboardingTaskTemplate
            {
                Title = task.Title.Trim(),
                DueDayOffset = task.DueDayOffset,
                DisplayOrder = order++
            });
        }

        _context.OnboardingTemplates.Add(template);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Template '{template.Name}' created.";
        return RedirectToAction(nameof(Templates));
    }

    private Task<List<Employee>> EmployeesAsync() =>
        _context.Employees
            .AsNoTracking()
            .Where(e => e.Status != EmploymentStatus.Exited)
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .Select(e => new Employee { Id = e.Id, FirstName = e.FirstName, LastName = e.LastName, EmployeeCode = e.EmployeeCode, JobTitle = e.JobTitle })
            .ToListAsync();

    private Task<List<OnboardingTemplate>> TemplateOptionsAsync() =>
        _context.OnboardingTemplates
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new OnboardingTemplate { Id = t.Id, Name = t.Name, Description = t.Description })
            .ToListAsync();

    private string? CurrentUserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
}