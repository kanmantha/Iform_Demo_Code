using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsersController(
        ApplicationDbContext context,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _context.Users.AsNoTracking().OrderBy(u => u.FullName).ToListAsync();
        var model = new List<UserListViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            model.Add(new UserListViewModel
            {
                User = new AppUserViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Department = user.Department,
                    JobTitle = user.JobTitle,
                    CreatedAt = user.CreatedAt
                },
                CurrentRole = roles.FirstOrDefault() ?? DbSeeder.UserRole
            });
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Roles = await RoleOptionsAsync();
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegisterViewModel model, string role = "User")
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await RoleOptionsAsync(role);
            return View(model);
        }

        var user = new AppUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            Department = model.Department,
            JobTitle = model.JobTitle,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }

            await _userManager.AddToRoleAsync(user, role);
            TempData["Success"] = $"User {user.FullName} created with the {role} role.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        ViewBag.Roles = await RoleOptionsAsync(role);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(string id, string role)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "You cannot change your own role.";
            return RedirectToAction(nameof(Index));
        }

        if (!await _roleManager.RoleExistsAsync(role))
        {
            await _roleManager.CreateAsync(new IdentityRole(role));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        await _userManager.AddToRoleAsync(user, role);
        TempData["Success"] = $"{user.FullName} is now {role}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.DeleteAsync(user);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? $"{user.FullName} deleted."
            : "Failed to delete user.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IEnumerable<string>> RoleOptionsAsync(string? selected = null)
    {
        var roles = await _roleManager.Roles.OrderBy(r => r.Name).Select(r => r.Name!).ToListAsync();
        if (roles.Count == 0)
        {
            roles = new List<string> { DbSeeder.AdminRole, DbSeeder.ManagerRole, DbSeeder.UserRole };
        }

        if (selected is not null && !roles.Contains(selected))
        {
            roles.Add(selected);
        }

        return roles;
    }
}
