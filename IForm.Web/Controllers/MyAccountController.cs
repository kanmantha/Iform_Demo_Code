using IForm.Web.Data;
using IForm.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IForm.Web.Controllers;

[Authorize]
public class MyAccountController : Controller
{
    private readonly ApplicationDbContext _context;

    public MyAccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var employee = await _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.AppUserId == userId);

        if (employee is null)
        {
            TempData["Error"] = "Your account is not linked to an employee record yet. Ask an admin to link it explicitly.";
            return RedirectToAction("Index", "Home");
        }

        return View(employee);
    }
}
