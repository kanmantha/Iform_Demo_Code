using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize]
public class MaterialCertificatesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public MaterialCertificatesController(
        ApplicationDbContext context,
        UserManager<AppUser> userManager,
        IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _environment = environment;
    }

    public async Task<IActionResult> Index(string? search = null, int page = 1)
    {
        const int pageSize = 10;

        var query = _context.MaterialCertificates
            .AsNoTracking()
            .Include(c => c.UploadedBy)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c =>
                c.CertificateNumber.Contains(search) ||
                c.Supplier.Contains(search) ||
                (c.WorkOrderNo != null && c.WorkOrderNo.Contains(search)) ||
                (c.Alloy != null && c.Alloy.Contains(search)));
        }

        query = query.OrderByDescending(c => c.CreatedAt);

        page = Math.Max(1, page);
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var certificates = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var model = new MaterialCertificateListViewModel
        {
            Search = search,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Certificates = certificates
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var cert = await _context.MaterialCertificates
            .AsNoTracking()
            .Include(c => c.UploadedBy)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cert is null)
        {
            return NotFound();
        }

        var model = new MaterialCertificateDetailViewModel
        {
            Certificate = cert,
            CanEdit = User.IsInRole(DbSeeder.AdminRole) || User.IsInRole(DbSeeder.ManagerRole)
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new MaterialCertificateFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MaterialCertificateFormViewModel model, IFormFile? file)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var nextNumber = (await _context.MaterialCertificates.MaxAsync(c => (int?)c.Id)) + 1 ?? 1;

        string? filePath = null;
        string? fileName = null;

        if (file is not null && file.Length > 0)
        {
            if (TryGetFileError(file, out var error))
            {
                ModelState.AddModelError(string.Empty, error);
                return View(model);
            }

            filePath = await SaveFileAsync(file);
            fileName = file.FileName;
        }

        var cert = new MaterialCertificate
        {
            CertificateNumber = $"MTC-{nextNumber:D2}",
            Supplier = model.Supplier.Trim(),
            SupplierReportNo = model.SupplierReportNo?.Trim(),
            WorkOrderNo = model.WorkOrderNo?.Trim(),
            Alloy = model.Alloy?.Trim(),
            TestMethod = model.TestMethod?.Trim(),
            ReportDate = ToUtcOrNull(model.ReportDate),
            SampleReceivedDate = ToUtcOrNull(model.SampleReceivedDate),
            TestDate = ToUtcOrNull(model.TestDate),
            ValidUntil = ToUtcOrNull(model.ValidUntil),
            Remarks = model.Remarks?.Trim(),
            FilePath = filePath,
            FileName = fileName,
            UploadedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.MaterialCertificates.Add(cert);
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            UserName = user.FullName,
            Action = "Create",
            EntityType = "MaterialCertificate",
            EntityId = cert.CertificateNumber,
            Details = $"Certificate {cert.CertificateNumber} created for {cert.Supplier}",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Certificate {cert.CertificateNumber} created for {cert.Supplier}.";
        return RedirectToAction(nameof(Details), new { id = cert.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var cert = await _context.MaterialCertificates
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cert is null)
        {
            return NotFound();
        }

        var model = new MaterialCertificateFormViewModel
        {
            Id = cert.Id,
            CertificateNumber = cert.CertificateNumber,
            Supplier = cert.Supplier,
            SupplierReportNo = cert.SupplierReportNo,
            WorkOrderNo = cert.WorkOrderNo,
            Alloy = cert.Alloy,
            TestMethod = cert.TestMethod,
            ReportDate = cert.ReportDate?.ToLocalTime(),
            SampleReceivedDate = cert.SampleReceivedDate?.ToLocalTime(),
            TestDate = cert.TestDate?.ToLocalTime(),
            ValidUntil = cert.ValidUntil?.ToLocalTime(),
            Remarks = cert.Remarks
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MaterialCertificateFormViewModel model, IFormFile? file)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var cert = await _context.MaterialCertificates.FirstOrDefaultAsync(c => c.Id == model.Id);
        if (cert is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);

        cert.Supplier = model.Supplier.Trim();
        cert.SupplierReportNo = model.SupplierReportNo?.Trim();
        cert.WorkOrderNo = model.WorkOrderNo?.Trim();
        cert.Alloy = model.Alloy?.Trim();
        cert.TestMethod = model.TestMethod?.Trim();
        cert.ReportDate = ToUtcOrNull(model.ReportDate);
        cert.SampleReceivedDate = ToUtcOrNull(model.SampleReceivedDate);
        cert.TestDate = ToUtcOrNull(model.TestDate);
        cert.ValidUntil = ToUtcOrNull(model.ValidUntil);
        cert.Remarks = model.Remarks?.Trim();
        cert.UpdatedAt = DateTime.UtcNow;

        if (file is not null && file.Length > 0)
        {
            if (TryGetFileError(file, out var error))
            {
                ModelState.AddModelError(string.Empty, error);
                return View(model);
            }

            cert.FilePath = await SaveFileAsync(file);
            cert.FileName = file.FileName;
        }

        if (user is not null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Action = "Update",
                EntityType = "MaterialCertificate",
                EntityId = cert.CertificateNumber,
                Details = $"Certificate {cert.CertificateNumber} updated",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Certificate {cert.CertificateNumber} updated.";
        return RedirectToAction(nameof(Details), new { id = cert.Id });
    }

    public async Task<IActionResult> Download(int id)
    {
        var cert = await _context.MaterialCertificates
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cert is null || cert.FilePath is null)
        {
            return NotFound();
        }

        var fullPath = Path.Combine(_environment.WebRootPath, cert.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound();
        }

        var contentType = cert.FileName?.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) == true
            ? "application/pdf"
            : "application/octet-stream";

        return PhysicalFile(fullPath, contentType, cert.FileName ?? Path.GetFileName(fullPath));
    }

    private static readonly string[] AllowedFileExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileBytes = 10 * 1024 * 1024;

    private static bool TryGetFileError(IFormFile file, out string error)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedFileExtensions.Contains(extension))
        {
            error = $"Unsupported file type \"{extension}\". Allowed: {string.Join(", ", AllowedFileExtensions)}.";
            return true;
        }

        if (file.Length > MaxFileBytes)
        {
            error = "File exceeds the 10 MB limit.";
            return true;
        }

        error = string.Empty;
        return false;
    }

    private async Task<string> SaveFileAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var folder = Path.Combine(_environment.WebRootPath, "uploads", "certificates");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folder, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/certificates/{fileName}";
    }

    private static DateTime? ToUtcOrNull(DateTime? value)
        => value.HasValue
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Local).ToUniversalTime()
            : null;
}
