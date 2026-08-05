using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class SitesController : Controller
{
    private readonly ApplicationDbContext _context;

    public SitesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var sites = await _context.Sites.AsNoTracking().OrderBy(s => s.Name).ToListAsync();

        var ticketCounts = await _context.Tickets
            .GroupBy(t => t.SiteId)
            .Select(g => new { SiteId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SiteId ?? -1, x => x.Count);

        var incidentCounts = await _context.Incidents
            .GroupBy(i => i.SiteId)
            .Select(g => new { SiteId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SiteId ?? -1, x => x.Count);

        ViewBag.TicketCounts = ticketCounts;
        ViewBag.IncidentCounts = incidentCounts;
        return View(sites);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new SiteFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SiteFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _context.Sites.AnyAsync(s => s.Code == model.Code.Trim()))
        {
            ModelState.AddModelError("Code", "A site with this code already exists.");
            return View(model);
        }

        _context.Sites.Add(new Site
        {
            Code = model.Code.Trim().ToUpperInvariant(),
            Name = model.Name.Trim(),
            Address = model.Address,
            City = model.City,
            Country = model.Country,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Site '{model.Name}' created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var site = await _context.Sites.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (site is null)
        {
            return NotFound();
        }

        return View(new SiteFormViewModel
        {
            Id = site.Id,
            Code = site.Code,
            Name = site.Name,
            Address = site.Address,
            City = site.City,
            Country = site.Country,
            IsActive = site.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SiteFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var site = await _context.Sites.FirstOrDefaultAsync(s => s.Id == model.Id);
        if (site is null)
        {
            return NotFound();
        }

        site.Code = model.Code.Trim().ToUpperInvariant();
        site.Name = model.Name.Trim();
        site.Address = model.Address;
        site.City = model.City;
        site.Country = model.Country;
        site.IsActive = model.IsActive;

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Site '{site.Name}' updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var site = await _context.Sites.FirstOrDefaultAsync(s => s.Id == id);
        if (site is null)
        {
            return NotFound();
        }

        site.IsActive = !site.IsActive;
        await _context.SaveChangesAsync();
        TempData["Success"] = site.IsActive ? $"Site '{site.Name}' activated." : $"Site '{site.Name}' deactivated.";
        return RedirectToAction(nameof(Index));
    }
}
