using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class OrganizationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public OrganizationController(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var units = await _context.OrganizationUnits
            .AsNoTracking()
            .Include(u => u.Parent)
            .Include(u => u.Manager)
            .Include(u => u.Members)
            .OrderBy(u => u.Name)
            .ToListAsync();

        return View(units);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new OrganizationUnitFormViewModel
        {
            Parents = await ParentOptionsAsync(),
            Managers = await ManagerOptionsAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrganizationUnitFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Parents = await ParentOptionsAsync(model.ParentId);
            model.Managers = await ManagerOptionsAsync(model.ManagerId);
            return View(model);
        }

        if (await _context.OrganizationUnits.AnyAsync(u => u.Code == model.Code.Trim()))
        {
            ModelState.AddModelError("Code", "A unit with this code already exists.");
            model.Parents = await ParentOptionsAsync(model.ParentId);
            model.Managers = await ManagerOptionsAsync(model.ManagerId);
            return View(model);
        }

        _context.OrganizationUnits.Add(new OrganizationUnit
        {
            Code = model.Code.Trim().ToUpperInvariant(),
            Name = model.Name.Trim(),
            Description = model.Description,
            ParentId = model.ParentId,
            ManagerId = model.ManagerId,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Department '{model.Name}' created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var unit = await _context.OrganizationUnits.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (unit is null)
        {
            return NotFound();
        }

        var model = new OrganizationUnitFormViewModel
        {
            Id = unit.Id,
            Code = unit.Code,
            Name = unit.Name,
            Description = unit.Description,
            ParentId = unit.ParentId,
            ManagerId = unit.ManagerId,
            IsActive = unit.IsActive,
            Parents = await ParentOptionsAsync(unit.ParentId, unit.Id),
            Managers = await ManagerOptionsAsync(unit.ManagerId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(OrganizationUnitFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Parents = await ParentOptionsAsync(model.ParentId);
            model.Managers = await ManagerOptionsAsync(model.ManagerId);
            return View(model);
        }

        if (model.ParentId == model.Id)
        {
            ModelState.AddModelError("ParentId", "A unit cannot be its own parent.");
            model.Parents = await ParentOptionsAsync(model.ParentId);
            model.Managers = await ManagerOptionsAsync(model.ManagerId);
            return View(model);
        }

        var unit = await _context.OrganizationUnits.FirstOrDefaultAsync(u => u.Id == model.Id);
        if (unit is null)
        {
            return NotFound();
        }

        unit.Code = model.Code.Trim().ToUpperInvariant();
        unit.Name = model.Name.Trim();
        unit.Description = model.Description;
        unit.ParentId = model.ParentId;
        unit.ManagerId = model.ManagerId;
        unit.IsActive = model.IsActive;

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Department '{unit.Name}' updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Members(int id)
    {
        var unit = await _context.OrganizationUnits.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (unit is null)
        {
            return NotFound();
        }

        var members = await _context.Users
            .AsNoTracking()
            .Where(u => u.OrganizationUnitId == id)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        var allUsers = await _context.Users.AsNoTracking().OrderBy(u => u.FullName).ToListAsync();

        ViewBag.Unit = unit;
        ViewBag.Members = members;
        ViewBag.AllUsers = allUsers;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Members(int id, List<string> memberIds)
    {
        var unit = await _context.OrganizationUnits.FirstOrDefaultAsync(u => u.Id == id);
        if (unit is null)
        {
            return NotFound();
        }

        var memberSet = memberIds ?? new List<string>();
        var allUsers = await _context.Users.ToListAsync();

        foreach (var user in allUsers)
        {
            var isMember = memberSet.Contains(user.Id);
            var currentUnit = user.OrganizationUnitId;
            if (isMember && currentUnit != id)
            {
                user.OrganizationUnitId = id;
                await _userManager.UpdateAsync(user);
            }
            else if (!isMember && currentUnit == id)
            {
                user.OrganizationUnitId = null;
                await _userManager.UpdateAsync(user);
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Members updated for {unit.Name}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var unit = await _context.OrganizationUnits.FirstOrDefaultAsync(u => u.Id == id);
        if (unit is null)
        {
            return NotFound();
        }

        unit.IsActive = !unit.IsActive;
        await _context.SaveChangesAsync();
        TempData["Success"] = unit.IsActive ? $"Department '{unit.Name}' activated." : $"Department '{unit.Name}' deactivated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IEnumerable<SelectListItem>> ParentOptionsAsync(int? selectedId = null, int? excludeId = null)
    {
        var units = await _context.OrganizationUnits
            .AsNoTracking()
            .Where(u => !excludeId.HasValue || u.Id != excludeId.Value)
            .OrderBy(u => u.Name)
            .ToListAsync();

        return units.Select(u => new SelectListItem($"{u.Code} - {u.Name}", u.Id.ToString(), u.Id == selectedId));
    }

    private async Task<IEnumerable<SelectListItem>> ManagerOptionsAsync(string? selectedId = null)
    {
        var users = await _context.Users.AsNoTracking().OrderBy(u => u.FullName).ToListAsync();
        return users.Select(u => new SelectListItem($"{u.FullName} ({u.Email})", u.Id, u.Id == selectedId));
    }
}
