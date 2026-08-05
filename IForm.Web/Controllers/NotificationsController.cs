using IForm.Web.Data;
using IForm.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public NotificationsController(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var notifications = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Take(100)
            .ToListAsync();

        ViewData["ActiveMenu"] = "notifications";
        return View(notifications);
    }

    [HttpGet]
    public async Task<IActionResult> Count()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Json(new { count = 0 });
        }

        var count = await _context.Notifications.CountAsync(n => n.UserId == user.Id && !n.IsRead);
        return Json(new { count });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);
        if (notification is not null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        await _context.Notifications
            .Where(n => n.UserId == user.Id && !n.IsRead)
            .ExecuteUpdateAsync(n => n.SetProperty(x => x.IsRead, true));

        return RedirectToAction(nameof(Index));
    }
}
