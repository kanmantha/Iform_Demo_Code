using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class DocumentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public DocumentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(DocumentCategory? category, DocumentVisibility? visibility, string? search)
    {
        var query = _context.Documents.Include(d => d.Employee).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d => d.Title.Contains(term) || (d.Description != null && d.Description.Contains(term)));
        }

        if (category.HasValue)
        {
            query = query.Where(d => d.Category == category.Value);
        }

        if (visibility.HasValue)
        {
            query = query.Where(d => d.Visibility == visibility.Value);
        }

        var documents = await query.OrderByDescending(d => d.UploadedAt).ToListAsync();

        var model = new DocumentListViewModel
        {
            Documents = documents,
            Category = category,
            Visibility = visibility,
            Search = search,
            TotalCount = await _context.Documents.CountAsync()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new DocumentFormViewModel();
        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DocumentFormViewModel model)
    {
        await PopulateOptionsAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.EmployeeId.HasValue && !await _context.Employees.AnyAsync(e => e.Id == model.EmployeeId.Value))
        {
            ModelState.AddModelError(nameof(model.EmployeeId), "Select a valid employee.");
            return View(model);
        }

        var document = new Document
        {
            Title = model.Title.Trim(),
            Description = model.Description,
            FilePath = model.FilePath.Trim(),
            FileType = model.FileType,
            FileSize = model.FileSize,
            Category = model.Category,
            Visibility = model.Visibility,
            EmployeeId = model.EmployeeId,
            UploadedById = User.Identity?.Name,
            UploadedAt = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Document created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var document = await _context.Documents.Include(d => d.Employee).AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (document is null)
        {
            return NotFound();
        }
        return View(document);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var document = await _context.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (document is null)
        {
            return NotFound();
        }

        var model = new DocumentFormViewModel
        {
            Id = document.Id,
            Title = document.Title,
            Description = document.Description,
            FilePath = document.FilePath,
            FileType = document.FileType,
            FileSize = document.FileSize,
            Category = document.Category,
            Visibility = document.Visibility,
            EmployeeId = document.EmployeeId,
            UploadedById = document.UploadedById
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(DocumentFormViewModel model)
    {
        await PopulateOptionsAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == model.Id);
        if (document is null)
        {
            return NotFound();
        }

        if (model.EmployeeId.HasValue && !await _context.Employees.AnyAsync(e => e.Id == model.EmployeeId.Value))
        {
            ModelState.AddModelError(nameof(model.EmployeeId), "Select a valid employee.");
            return View(model);
        }

        document.Title = model.Title.Trim();
        document.Description = model.Description;
        document.FilePath = model.FilePath.Trim();
        document.FileType = model.FileType;
        document.FileSize = model.FileSize;
        document.Category = model.Category;
        document.Visibility = model.Visibility;
        document.EmployeeId = model.EmployeeId;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Document updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id);
        if (document is null)
        {
            return NotFound();
        }

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Document deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(DocumentFormViewModel model)
    {
        model.Employees = await _context.Employees
            .AsNoTracking()
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .ToListAsync();
    }
}
