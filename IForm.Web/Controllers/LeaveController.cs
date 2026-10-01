using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.Services;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class LeaveController : Controller
{
    private const int MaxNumberLength = 30;

    private readonly ApplicationDbContext _context;

    public LeaveController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(LeaveStatus? status, LeaveType? leaveType, int? employeeId)
    {
        var query = _context.LeaveRequests
            .Include(r => r.Employee)
            .AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (leaveType.HasValue)
        {
            query = query.Where(r => r.LeaveType == leaveType.Value);
        }

        if (employeeId.HasValue)
        {
            query = query.Where(r => r.EmployeeId == employeeId.Value);
        }

        var model = new LeaveListViewModel
        {
            LeaveRequests = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(),
            Employees = await EmployeesAsync(),
            Status = status,
            LeaveType = leaveType,
            EmployeeId = employeeId,
            TotalCount = await _context.LeaveRequests.CountAsync(),
            PendingCount = await _context.LeaveRequests.CountAsync(r => r.Status == LeaveStatus.Pending),
            ApprovedCount = await _context.LeaveRequests.CountAsync(r => r.Status == LeaveStatus.Approved),
            RejectedCount = await _context.LeaveRequests.CountAsync(r => r.Status == LeaveStatus.Rejected)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new LeaveRequestFormViewModel { Employees = await EmployeesAsync() };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeaveRequestFormViewModel model)
    {
        model.Employees = await EmployeesAsync();
        model.Days = LeaveCalculator.CountWorkingDays(model.StartDate, model.EndDate);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.StartDate.Date > model.EndDate.Date)
        {
            ModelState.AddModelError(nameof(model.EndDate), "End date must be on or after the start date.");
            return View(model);
        }

        if (model.Days <= 0m)
        {
            ModelState.AddModelError(nameof(model.StartDate), "That range contains no working days.");
            return View(model);
        }

        if (await HasOverlappingLeaveAsync(model.EmployeeId, model.StartDate, model.EndDate, excludeId: null))
        {
            ModelState.AddModelError(nameof(model.StartDate), "The employee already has leave in that range.");
            return View(model);
        }

        model.AvailableBalance = await BalanceAsync(model.EmployeeId, model.LeaveType);

        var request = new LeaveRequest
        {
            RequestNumber = await NextRequestNumberAsync(),
            EmployeeId = model.EmployeeId,
            LeaveType = model.LeaveType,
            StartDate = model.StartDate.Date,
            EndDate = model.EndDate.Date,
            Days = model.Days,
            Reason = model.Reason.Trim(),
            Status = LeaveStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.LeaveRequests.Add(request);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Leave request {request.RequestNumber} submitted for approval.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var request = await _context.LeaveRequests
            .AsNoTracking()
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request is null)
        {
            return NotFound();
        }

        var ledger = await _context.LeaveLedgerEntries
            .AsNoTracking()
            .Where(l => l.EmployeeId == request.EmployeeId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(10)
            .Select(l => new LeaveLedgerRow(l.CreatedAt, l.LeaveType, l.Days, l.Reason))
            .ToListAsync();

        return View(new LeaveDetailViewModel
        {
            Request = request,
            BalanceBeforeDecision = await BalanceAsync(request.EmployeeId, request.LeaveType),
            RecentLedger = ledger
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var request = await _context.LeaveRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != LeaveStatus.Pending)
        {
            TempData["Error"] = $"This request was already {request.Status.ToString().ToLowerInvariant()}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var balance = await BalanceAsync(request.EmployeeId, request.LeaveType);
        var overlap = await HasOverlappingLeaveAsync(
            request.EmployeeId, request.StartDate, request.EndDate, excludeId: request.Id, approvedOnly: true);

        var problem = LeaveCalculator.ValidateApproval(balance, request.Days, overlap);
        if (problem is not null)
        {
            TempData["Error"] = problem;
            return RedirectToAction(nameof(Details), new { id });
        }

        request.Status = LeaveStatus.Approved;
        request.DecidedAt = DateTime.UtcNow;
        request.DecidedById = CurrentUserId();

        _context.LeaveLedgerEntries.Add(new LeaveLedgerEntry
        {
            EmployeeId = request.EmployeeId,
            LeaveType = request.LeaveType,
            Days = -request.Days,
            Reason = $"Approved leave {request.RequestNumber}",
            LeaveRequestId = request.Id,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = $"{request.RequestNumber} approved. {request.Days:0.##} day(s) deducted.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? note)
    {
        var request = await _context.LeaveRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != LeaveStatus.Pending)
        {
            TempData["Error"] = $"This request was already {request.Status.ToString().ToLowerInvariant()}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        request.Status = LeaveStatus.Rejected;
        request.DecidedAt = DateTime.UtcNow;
        request.DecidedById = CurrentUserId();
        request.DecisionNote = string.IsNullOrWhiteSpace(note) ? "Rejected by approver." : note.Trim();

        await _context.SaveChangesAsync();

        TempData["Success"] = $"{request.RequestNumber} rejected.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Balance as the sum of the ledger. No ledger means no entitlement, so zero.</summary>
    private async Task<decimal> BalanceAsync(int employeeId, LeaveType leaveType) =>
        await _context.LeaveLedgerEntries
            .Where(l => l.EmployeeId == employeeId && l.LeaveType == leaveType)
            .SumAsync(l => (decimal?)l.Days) ?? 0m;

    private async Task<bool> HasOverlappingLeaveAsync(
        int employeeId, DateTime start, DateTime end, int? excludeId, bool approvedOnly = false)
    {
        var query = _context.LeaveRequests.Where(r =>
            r.EmployeeId == employeeId &&
            r.StartDate <= end &&
            r.EndDate >= start);

        if (excludeId.HasValue)
        {
            query = query.Where(r => r.Id != excludeId.Value);
        }

        query = approvedOnly
            ? query.Where(r => r.Status == LeaveStatus.Approved)
            : query.Where(r => r.Status == LeaveStatus.Approved || r.Status == LeaveStatus.Pending);

        return await query.AnyAsync();
    }

    private async Task<string> NextRequestNumberAsync()
    {
        var count = await _context.LeaveRequests.CountAsync();
        var candidate = $"LV-{count + 1:D5}";

        // Keep stepping in case a number was used and then removed.
        var clash = await _context.LeaveRequests.AnyAsync(r => r.RequestNumber == candidate);
        while (clash)
        {
            count++;
            candidate = $"LV-{count:D5}";
            clash = await _context.LeaveRequests.AnyAsync(r => r.RequestNumber == candidate);
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