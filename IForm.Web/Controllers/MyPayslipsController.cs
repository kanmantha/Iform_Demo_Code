using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize]
public class MyPayslipsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public MyPayslipsController(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(PayslipStatus? status, int? year, int? month)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var employee = await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.AppUserId == user.Id);
        if (employee is null)
        {
            return NotFound();
        }

        var query = _context.Payslips.AsNoTracking().Where(p => p.EmployeeId == employee.Id);

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (year.HasValue)
        {
            query = query.Where(p => p.Year == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(p => p.Month == month.Value.ToString("00") || p.Month == month.Value.ToString());
        }

        var payslips = await query.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToListAsync();

        var model = new MyPayslipListViewModel
        {
            Payslips = payslips,
            Status = status,
            Year = year,
            Month = month,
            TotalCount = await query.CountAsync()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var employee = await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.AppUserId == user.Id);
        if (employee is null)
        {
            return NotFound();
        }

        var payslip = await _context.Payslips.Include(p => p.Employee).AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.EmployeeId == employee.Id);
        if (payslip is null)
        {
            return NotFound();
        }
        return View(payslip);
    }
}
