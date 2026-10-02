using IForm.Web.Data;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize]
public class DirectoryController : Controller
{
    private readonly ApplicationDbContext _context;

    public DirectoryController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.DirectoryEntries.Include(d => d.Employee).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(d => d.Employee != null && (
                d.Employee.FirstName.ToLower().Contains(term) ||
                d.Employee.LastName.ToLower().Contains(term) ||
                (d.Employee.Email != null && d.Employee.Email.ToLower().Contains(term)) ||
                (d.Employee.JobTitle != null && d.Employee.JobTitle.ToLower().Contains(term))
            ));
        }

        var entries = await query.ToListAsync();
        entries = entries.OrderBy(d => d.Employee?.LastName ?? string.Empty).ThenBy(d => d.Employee?.FirstName ?? string.Empty).ToList();

        var model = new DirectoryListViewModel
        {
            Entries = entries,
            Search = search,
            TotalCount = await _context.DirectoryEntries.CountAsync()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var entry = await _context.DirectoryEntries.Include(d => d.Employee).AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);
        if (entry is null)
        {
            return NotFound();
        }
        return View(entry);
    }
}
