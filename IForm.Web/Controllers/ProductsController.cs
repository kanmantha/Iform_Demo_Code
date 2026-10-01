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
    private readonly IWebHostEnvironment _environment;

    public ProductsController(ApplicationDbContext context, UserManager<AppUser> userManager, IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
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

        if (model.ImageFile is not null && model.ImageFile.Length > 0)
        {
            if (TryGetImageError(model.ImageFile, out var imageError))
            {
                ModelState.AddModelError(nameof(model.ImageFile), imageError);
                return View(model);
            }

            product.ImagePath = await SaveImageAsync(model.ImageFile);
        }

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
            ImagePath = product.ImagePath,
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

        if (model.ImageFile is not null && model.ImageFile.Length > 0)
        {
            if (TryGetImageError(model.ImageFile, out var imageError))
            {
                ModelState.AddModelError(nameof(model.ImageFile), imageError);
                return View(model);
            }

            var newImagePath = await SaveImageAsync(model.ImageFile);
            var oldImagePath = product.ImagePath;
            product.ImagePath = newImagePath;
            await _context.SaveChangesAsync();
            DeleteImageFile(oldImagePath);
            TempData["Success"] = $"Product {product.ProductCode} updated.";
            return RedirectToAction(nameof(Index));
        }

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
        var imagePath = product.ImagePath;
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
        DeleteImageFile(imagePath);

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

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const int MaxImageBytes = 5 * 1024 * 1024;

    private static bool TryGetImageError(IFormFile file, out string error)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(extension))
        {
            error = $"Unsupported image type \"{extension}\". Allowed: {string.Join(", ", AllowedImageExtensions)}.";
            return true;
        }

        if (file.Length > MaxImageBytes)
        {
            error = "Image exceeds the 5 MB limit.";
            return true;
        }

        error = string.Empty;
        return false;
    }

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                $"Unsupported image type \"{extension}\". Allowed: {string.Join(", ", AllowedImageExtensions)}.");
        }

        var folder = Path.Combine(_environment.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folder, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/products/{fileName}";
    }

    private void DeleteImageFile(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !imagePath.StartsWith("/uploads/products/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
    }
}
