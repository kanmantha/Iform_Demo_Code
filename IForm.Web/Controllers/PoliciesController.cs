using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.Services;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class PoliciesController : Controller
{
    private readonly ApplicationDbContext _context;

    public PoliciesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(PolicyStatus? status, string? search)
    {
        var query = _context.Policies.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p => p.Title.Contains(term) || (p.Description != null && p.Description.Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        var policies = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

        var model = new PolicyListViewModel
        {
            Policies = policies,
            Status = status,
            Search = search,
            TotalCount = await _context.Policies.CountAsync(),
            DraftCount = await _context.Policies.CountAsync(p => p.Status == PolicyStatus.Draft),
            PublishedCount = await _context.Policies.CountAsync(p => p.Status == PolicyStatus.Published),
            ArchivedCount = await _context.Policies.CountAsync(p => p.Status == PolicyStatus.Archived)
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new PolicyFormViewModel();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PolicyFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var policy = new Policy
        {
            Title = model.Title.Trim(),
            Description = model.Description,
            FilePath = model.FilePath,
            Status = model.Status,
            CreatedAt = DateTime.UtcNow
        };

        if (policy.Status == PolicyStatus.Published && policy.PublishedAt == null)
        {
            policy.PublishedAt = DateTime.UtcNow;
            policy.PublishedById = User.Identity?.Name;
        }

        _context.Policies.Add(policy);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Policy created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var policy = await _context.Policies.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (policy is null)
        {
            return NotFound();
        }
        return View(policy);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var policy = await _context.Policies.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (policy is null)
        {
            return NotFound();
        }

        var model = new PolicyFormViewModel
        {
            Id = policy.Id,
            Title = policy.Title,
            Description = policy.Description,
            FilePath = policy.FilePath,
            Status = policy.Status
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PolicyFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var policy = await _context.Policies.FirstOrDefaultAsync(p => p.Id == model.Id);
        if (policy is null)
        {
            return NotFound();
        }

        policy.Title = model.Title.Trim();
        policy.Description = model.Description;
        policy.FilePath = model.FilePath;
        policy.Status = model.Status;

        if (policy.Status == PolicyStatus.Published && policy.PublishedAt == null)
        {
            policy.PublishedAt = DateTime.UtcNow;
            policy.PublishedById = User.Identity?.Name;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Policy updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var policy = await _context.Policies.FirstOrDefaultAsync(p => p.Id == id);
        if (policy is null)
        {
            return NotFound();
        }

        _context.Policies.Remove(policy);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Policy deleted.";
        return RedirectToAction(nameof(Index));
    }
}
