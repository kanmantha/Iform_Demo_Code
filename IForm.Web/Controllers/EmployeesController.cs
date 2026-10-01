using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class EmployeesController : Controller
{
    private readonly ApplicationDbContext _context;

    public EmployeesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, int? departmentId, EmploymentStatus? status)
    {
        var query = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e =>
                e.FirstName.Contains(term) ||
                e.LastName.Contains(term) ||
                e.Email.Contains(term) ||
                e.EmployeeCode.Contains(term));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(e => e.DepartmentId == departmentId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        var employees = await query
            .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
            .ToListAsync();

        var model = new EmployeeListViewModel
        {
            Employees = employees,
            Departments = await _context.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync(),
            Search = search,
            DepartmentId = departmentId,
            Status = status,
            TotalCount = await _context.Employees.CountAsync(),
            ActiveCount = await _context.Employees.CountAsync(e => e.Status == EmploymentStatus.Active),
            ExitedCount = await _context.Employees.CountAsync(e => e.Status == EmploymentStatus.Exited)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new EmployeeFormViewModel { DateJoined = DateTime.UtcNow.Date };
        await PopulateOptionsAsync(model, excludeId: null);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormViewModel model)
    {
        await PopulateOptionsAsync(model, excludeId: null);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var code = model.EmployeeCode.Trim().ToUpperInvariant();
        if (await _context.Employees.AnyAsync(e => e.EmployeeCode == code))
        {
            ModelState.AddModelError(nameof(model.EmployeeCode), "An employee with this code already exists.");
            return View(model);
        }

        var email = model.Email.Trim().ToLowerInvariant();
        if (await _context.Employees.AnyAsync(e => e.Email == email))
        {
            ModelState.AddModelError(nameof(model.Email), "An employee with this email already exists.");
            return View(model);
        }

        if (model.ManagerId.HasValue && !await _context.Employees.AnyAsync(e => e.Id == model.ManagerId.Value))
        {
            ModelState.AddModelError(nameof(model.ManagerId), "Select an existing manager.");
            return View(model);
        }

        var employee = new Employee
        {
            EmployeeCode = code,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            Email = email,
            Phone = model.Phone,
            DepartmentId = model.DepartmentId,
            JobTitle = model.JobTitle,
            ManagerId = model.ManagerId,
            Status = model.Status,
            DateJoined = model.DateJoined,
            DateOfBirth = model.DateOfBirth,
            Address = model.Address,
            EmergencyContactName = model.EmergencyContactName,
            EmergencyContactPhone = model.EmergencyContactPhone,
            CreatedAt = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Employee '{employee.FullName}' added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null)
        {
            return NotFound();
        }

        var reports = await _context.Employees
            .AsNoTracking()
            .Where(e => e.ManagerId == id)
            .OrderBy(e => e.FirstName)
            .ToListAsync();

        ViewBag.DirectReports = reports;
        return View(employee);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var employee = await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (employee is null)
        {
            return NotFound();
        }

        var model = new EmployeeFormViewModel
        {
            Id = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            Phone = employee.Phone,
            DepartmentId = employee.DepartmentId,
            JobTitle = employee.JobTitle,
            ManagerId = employee.ManagerId,
            Status = employee.Status,
            DateJoined = employee.DateJoined,
            DateOfBirth = employee.DateOfBirth,
            Address = employee.Address,
            EmergencyContactName = employee.EmergencyContactName,
            EmergencyContactPhone = employee.EmergencyContactPhone
        };

        await PopulateOptionsAsync(model, excludeId: employee.Id);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EmployeeFormViewModel model)
    {
        await PopulateOptionsAsync(model, excludeId: model.Id);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == model.Id);
        if (employee is null)
        {
            return NotFound();
        }

        var code = model.EmployeeCode.Trim().ToUpperInvariant();
        if (await _context.Employees.AnyAsync(e => e.EmployeeCode == code && e.Id != model.Id))
        {
            ModelState.AddModelError(nameof(model.EmployeeCode), "An employee with this code already exists.");
            return View(model);
        }

        var email = model.Email.Trim().ToLowerInvariant();
        if (await _context.Employees.AnyAsync(e => e.Email == email && e.Id != model.Id))
        {
            ModelState.AddModelError(nameof(model.Email), "An employee with this email already exists.");
            return View(model);
        }

        if (model.ManagerId.HasValue)
        {
            if (model.ManagerId == model.Id)
            {
                ModelState.AddModelError(nameof(model.ManagerId), "An employee cannot report to themselves.");
                return View(model);
            }

            if (await CreatesReportingCycleAsync(model.Id, model.ManagerId.Value))
            {
                ModelState.AddModelError(nameof(model.ManagerId), "That manager already reports to this employee, which would create a loop.");
                return View(model);
            }
        }

        employee.EmployeeCode = code;
        employee.FirstName = model.FirstName.Trim();
        employee.LastName = model.LastName.Trim();
        employee.Email = email;
        employee.Phone = model.Phone;
        employee.DepartmentId = model.DepartmentId;
        employee.JobTitle = model.JobTitle;
        employee.ManagerId = model.ManagerId;
        employee.Status = model.Status;
        employee.DateJoined = model.DateJoined;
        employee.DateOfBirth = model.DateOfBirth;
        employee.Address = model.Address;
        employee.EmergencyContactName = model.EmergencyContactName;
        employee.EmergencyContactPhone = model.EmergencyContactPhone;

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Employee '{employee.FullName}' updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, EmploymentStatus status)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (employee is null)
        {
            return NotFound();
        }

        employee.Status = status;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"'{employee.FullName}' is now {status}.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Walks up from <paramref name="candidateManagerId"/> to see whether
    /// <paramref name="employeeId"/> appears, which would close a reporting loop.
    /// </summary>
    private async Task<bool> CreatesReportingCycleAsync(int employeeId, int candidateManagerId)
    {
        var current = candidateManagerId;
        var guard = 0;

        while (current > 0 && guard < 50)
        {
            if (current == employeeId)
            {
                return true;
            }

            var managerId = await _context.Employees
                .Where(e => e.Id == current)
                .Select(e => e.ManagerId)
                .FirstOrDefaultAsync();

            if (managerId is null)
            {
                return false;
            }

            current = managerId.Value;
            guard++;
        }

        return false;
    }

    private async Task PopulateOptionsAsync(EmployeeFormViewModel model, int? excludeId)
    {
        model.Departments = await _context.Departments
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .ToListAsync();

        model.Managers = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id != (excludeId ?? 0) && e.Status != EmploymentStatus.Exited)
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .Select(e => new Employee { Id = e.Id, FirstName = e.FirstName, LastName = e.LastName, JobTitle = e.JobTitle })
            .ToListAsync();
    }
}