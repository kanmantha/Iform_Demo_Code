using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize]
public class ProductsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public ProductsController(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search, string? family)
    {
        var query = _context.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(p =>
                p.ProductCode.Contains(search) ||
                p.Name.Contains(search) ||
                p.Family!.Contains(search) ||
                p.Material!.Contains(search) ||
                p.Specification!.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(family)) query = query.Where(p => p.Family == family);

        var products = await query.OrderBy(p => p.ProductCode).ToListAsync();

        var model = new ProductListViewModel
        {
            Search = search,
            Family = family,
            Products = products,
            FamilyOptions = await FamilyOptionsAsync()
        };

        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public IActionResult Create()
    {
        return View(new ProductFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existing = await _context.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductCode.ToLower() == model.ProductCode.Trim().ToLower());
        if (existing is not null)
        {
            ModelState.AddModelError(nameof(model.ProductCode), "A product with this code already exists.");
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        var product = new Product
        {
            ProductCode = model.ProductCode.Trim(),
            Name = model.Name.Trim(),
            Family = model.Family?.Trim(),
            Material = model.Material?.Trim(),
            Specification = model.Specification?.Trim(),
            Dimensions = model.Dimensions?.Trim(),
            Project = model.Project?.Trim(),
            Unit = model.Unit?.Trim(),
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        if (user is not null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Action = "Create",
                EntityType = "Product",
                EntityId = product.ProductCode,
                Details = $"Product {product.ProductCode} - {product.Name} added to catalogue",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Product {product.ProductCode} added to the catalogue.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        var model = new ProductFormViewModel
        {
            Id = product.Id,
            ProductCode = product.ProductCode,
            Name = product.Name,
            Family = product.Family,
            Material = product.Material,
            Specification = product.Specification,
            Dimensions = product.Dimensions,
            Project = product.Project,
            Unit = product.Unit,
            IsActive = product.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == model.Id);
        if (product is null)
        {
            return NotFound();
        }

        var existing = await _context.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductCode.ToLower() == model.ProductCode.Trim().ToLower() && p.Id != model.Id);
        if (existing is not null)
        {
            ModelState.AddModelError(nameof(model.ProductCode), "A product with this code already exists.");
            return View(model);
        }

        product.ProductCode = model.ProductCode.Trim();
        product.Name = model.Name.Trim();
        product.Family = model.Family?.Trim();
        product.Material = model.Material?.Trim();
        product.Specification = model.Specification?.Trim();
        product.Dimensions = model.Dimensions?.Trim();
        product.Project = model.Project?.Trim();
        product.Unit = model.Unit?.Trim();
        product.IsActive = model.IsActive;

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Product {product.ProductCode} updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        _context.Products.Remove(product);
        if (user is not null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Action = "Delete",
                EntityType = "Product",
                EntityId = product.ProductCode,
                Details = $"Product {product.ProductCode} removed from catalogue",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Product {product.ProductCode} removed.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IReadOnlyList<string>> FamilyOptionsAsync()
        => await _context.Products
            .AsNoTracking()
            .Where(p => !string.IsNullOrWhiteSpace(p.Family))
            .Select(p => p.Family!)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();
}
