using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.Services;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class ExpensesController : Controller
{
    private readonly ApplicationDbContext _context;

    public ExpensesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(ExpenseStatus? status, int? employeeId)
    {
        var query = _context.ExpenseClaims.Include(c => c.Employee).AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        if (employeeId.HasValue)
        {
            query = query.Where(c => c.EmployeeId == employeeId.Value);
        }

        var model = new ExpenseListViewModel
        {
            Claims = await query.OrderByDescending(c => c.CreatedAt).ToListAsync(),
            Employees = await EmployeesAsync(),
            Status = status,
            EmployeeId = employeeId,
            TotalCount = await _context.ExpenseClaims.CountAsync(),
            PendingCount = await _context.ExpenseClaims.CountAsync(c => c.Status == ExpenseStatus.Submitted),
            ApprovedCount = await _context.ExpenseClaims.CountAsync(c => c.Status == ExpenseStatus.Approved),
            RejectedCount = await _context.ExpenseClaims.CountAsync(c => c.Status == ExpenseStatus.Rejected),
            ReimbursedTotal = await _context.ExpenseClaims
                .Where(c => c.Status == ExpenseStatus.Reimbursed)
                .SumAsync(c => (decimal?)c.Amount) ?? 0m
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(new ExpenseClaimFormViewModel { Employees = await EmployeesAsync() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExpenseClaimFormViewModel model)
    {
        model.Employees = await EmployeesAsync();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var claim = new ExpenseClaim
        {
            ClaimNumber = await NextClaimNumberAsync(),
            EmployeeId = model.EmployeeId,
            Category = model.Category.Trim(),
            Description = model.Description,
            Amount = model.Amount,
            Currency = model.Currency.Trim().ToUpperInvariant(),
            ExpenseDate = model.ExpenseDate.Date,
            Status = ExpenseStatus.Submitted,
            CreatedAt = DateTime.UtcNow
        };

        _context.ExpenseClaims.Add(claim);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Claim {claim.ClaimNumber} submitted for approval.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var claim = await _context.ExpenseClaims
            .AsNoTracking()
            .Include(c => c.Employee)
            .FirstOrDefaultAsync(c => c.Id == id);

        return claim is null ? NotFound() : View(claim);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var claim = await _context.ExpenseClaims.FirstOrDefaultAsync(c => c.Id == id);
        if (claim is null)
        {
            return NotFound();
        }

        if (claim.Status != ExpenseStatus.Submitted)
        {
            TempData["Error"] = $"This claim was already {claim.Status.ToString().ToLowerInvariant()}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        claim.Status = ExpenseStatus.Approved;
        claim.DecidedAt = DateTime.UtcNow;
        claim.DecidedById = CurrentUserId();
        await _context.SaveChangesAsync();

        TempData["Success"] = $"{claim.ClaimNumber} approved for reimbursement.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? note)
    {
        var claim = await _context.ExpenseClaims.FirstOrDefaultAsync(c => c.Id == id);
        if (claim is null)
        {
            return NotFound();
        }

        if (claim.Status != ExpenseStatus.Submitted)
        {
            TempData["Error"] = $"This claim was already {claim.Status.ToString().ToLowerInvariant()}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        claim.Status = ExpenseStatus.Rejected;
        claim.DecidedAt = DateTime.UtcNow;
        claim.DecidedById = CurrentUserId();
        claim.DecisionNote = string.IsNullOrWhiteSpace(note) ? "Rejected by approver." : note.Trim();
        await _context.SaveChangesAsync();

        TempData["Success"] = $"{claim.ClaimNumber} rejected.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Marks an approved claim as paid. Only approved claims can be reimbursed.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkReimbursed(int id)
    {
        var claim = await _context.ExpenseClaims.FirstOrDefaultAsync(c => c.Id == id);
        if (claim is null)
        {
            return NotFound();
        }

        if (claim.Status != ExpenseStatus.Approved)
        {
            TempData["Error"] = "Only an approved claim can be marked as reimbursed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        claim.Status = ExpenseStatus.Reimbursed;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"{claim.ClaimNumber} marked as reimbursed.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<string> NextClaimNumberAsync()
    {
        var count = await _context.ExpenseClaims.CountAsync();
        var candidate = $"EX-{count + 1:D5}";
        var clash = await _context.ExpenseClaims.AnyAsync(c => c.ClaimNumber == candidate);

        while (clash)
        {
            count++;
            candidate = $"EX-{count:D5}";
            clash = await _context.ExpenseClaims.AnyAsync(c => c.ClaimNumber == candidate);
        }

        return candidate;
    }

    private Task<List<Employee>> EmployeesAsync() =>
        _context.Employees
            .AsNoTracking()
            .Where(e => e.Status != EmploymentStatus.Exited)
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .Select(e => new Employee { Id = e.Id, FirstName = e.FirstName, LastName = e.LastName, EmployeeCode = e.EmployeeCode, JobTitle = e.JobTitle })
            .ToListAsync();

    private string? CurrentUserId() => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
}