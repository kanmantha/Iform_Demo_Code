using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.Services;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class AttendanceController : Controller
{
    private readonly ApplicationDbContext _context;

    public AttendanceController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to, int? employeeId, AttendanceStatus? status)
    {
        var query = _context.AttendanceRecords.Include(a => a.Employee).AsNoTracking();

        if (from.HasValue)
        {
            query = query.Where(a => a.Date >= from.Value.Date);
        }

        if (to.HasValue)
        {
            query = query.Where(a => a.Date <= to.Value.Date);
        }

        if (employeeId.HasValue)
        {
            query = query.Where(a => a.EmployeeId == employeeId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        var records = await query.OrderByDescending(a => a.Date).ToListAsync();

        var model = new AttendanceListViewModel
        {
            Records = records,
            Employees = await EmployeesAsync(),
            From = from,
            To = to,
            EmployeeId = employeeId,
            Status = status,
            TotalHours = records.Sum(r => r.HoursWorked),
            TotalCount = records.Count
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(new AttendanceFormViewModel { Employees = await EmployeesAsync() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AttendanceFormViewModel model)
    {
        model.Employees = await EmployeesAsync();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var problem = AttendanceCalculator.Validate(model.CheckIn, model.CheckOut);
        if (problem is not null)
        {
            ModelState.AddModelError(nameof(model.CheckIn), problem);
            return View(model);
        }

        if (await _context.AttendanceRecords.AnyAsync(a => a.EmployeeId == model.EmployeeId && a.Date == model.Date.Date))
        {
            ModelState.AddModelError(nameof(model.Date), "Attendance for that employee and date is already recorded.");
            return View(model);
        }

        var hours = AttendanceCalculator.HoursWorked(model.CheckIn, model.CheckOut);
        var status = AttendanceCalculator.StatusFor(hours);
        if (status is null)
        {
            ModelState.AddModelError(nameof(model.CheckOut), "Those times do not produce a valid working day.");
            return View(model);
        }

        var record = new AttendanceRecord
        {
            EmployeeId = model.EmployeeId,
            Date = model.Date.Date,
            CheckIn = model.CheckIn,
            CheckOut = model.CheckOut,
            HoursWorked = hours,
            Status = status.Value,
            Notes = model.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.AttendanceRecords.Add(record);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Attendance recorded: {hours:0.##} hours ({record.Status}).";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await _context.AttendanceRecords.FirstOrDefaultAsync(a => a.Id == id);
        if (record is null)
        {
            return NotFound();
        }

        _context.AttendanceRecords.Remove(record);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Attendance record deleted.";
        return RedirectToAction(nameof(Index));
    }

    private Task<List<Employee>> EmployeesAsync() =>
        _context.Employees
            .AsNoTracking()
            .Where(e => e.Status != EmploymentStatus.Exited)
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .Select(e => new Employee { Id = e.Id, FirstName = e.FirstName, LastName = e.LastName, EmployeeCode = e.EmployeeCode, JobTitle = e.JobTitle })
            .ToListAsync();
}