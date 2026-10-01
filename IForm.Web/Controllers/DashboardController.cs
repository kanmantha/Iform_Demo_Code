using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public DashboardController(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser is null)
        {
            return Challenge();
        }

        var userId = currentUser.Id;

        var tickets = await _context.Tickets
            .AsNoTracking()
            .Include(t => t.ReportedBy)
            .Include(t => t.AssignedTo)
            .Where(t => t.ReportedById == userId || t.AssignedToId == userId)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var incidents = await _context.Incidents
            .AsNoTracking()
            .Where(i => i.ReportedById == userId || i.AssignedToId == userId)
            .ToListAsync();
        var actions = await _context.ActionItems
            .AsNoTracking()
            .Where(a => a.AssignedToId == userId)
            .ToListAsync();

        var resolvedOrClosed = tickets.Where(t => t.Status is TicketStatus.Resolved or TicketStatus.Closed).ToList();

        var model = new DashboardViewModel
        {
            MyReportedTickets = tickets.Count(t => t.ReportedById == userId),
            MyOpenTickets = tickets.Count(t => t.Status == TicketStatus.New || t.Status == TicketStatus.Open),
            MyInProgressTickets = tickets.Count(t => t.Status == TicketStatus.InProgress || t.Status == TicketStatus.Pending),
            MyResolvedTickets = tickets.Count(t => t.Status == TicketStatus.Resolved),
            MyClosedTickets = tickets.Count(t => t.Status == TicketStatus.Closed),
            MyCriticalTickets = tickets.Count(t => t.Priority == TicketPriority.Critical && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed),
            MyAssignedTickets = tickets.Count(t => t.AssignedToId == userId && t.Status != TicketStatus.Closed),
            MyIncidents = incidents.Count,
            MyOpenIncidents = incidents.Count(i => i.Status != IncidentStatus.Closed),
            MyCriticalIncidents = incidents.Count(i => i.Severity == IncidentSeverity.Critical && i.Status != IncidentStatus.Closed),
            MyOpenActions = actions.Count(a => a.Status == ActionStatus.Open || a.Status == ActionStatus.InProgress),
            MyOverdueActions = actions.Count(a => a.Status != ActionStatus.Completed && a.Status != ActionStatus.Cancelled && a.DueDate < now),
            MyCompletedActions = actions.Count(a => a.Status == ActionStatus.Completed),
            ResolvedRate = tickets.Count == 0 ? 0 : Math.Round((double)resolvedOrClosed.Count / tickets.Count * 100, 1),
            AvgResolutionDays = resolvedOrClosed.Count == 0 ? 0 : Math.Round(resolvedOrClosed.Average(t =>
                ((t.ClosedAt ?? t.UpdatedAt ?? t.CreatedAt) - t.CreatedAt).TotalDays), 1),
            TicketsByStatus = tickets.GroupBy(t => t.Status).ToDictionary(g => g.Key.ToString(), g => g.Count()),
            TicketsByPriority = tickets.GroupBy(t => t.Priority).ToDictionary(g => g.Key.ToString(), g => g.Count()),
            TicketsByCategory = tickets.GroupBy(t => t.Category).ToDictionary(g => g.Key.ToString(), g => g.Count()),
            TicketsPerDay = BuildDailyTrend(tickets),
            RecentTickets = tickets
                .OrderByDescending(t => t.CreatedAt)
                .Take(8)
                .ToList(),
            RecentActivity = await _context.AuditLogs
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(8)
                .ToListAsync()
        };

        return View(model);
    }

    private static IReadOnlyList<DailyCount> BuildDailyTrend(List<Ticket> tickets)
    {
        var days = Enumerable.Range(0, 14).Select(i => DateTime.UtcNow.Date.AddDays(-(13 - i))).ToList();
        return days
            .Select(d => new DailyCount(d.ToString("MMM d"), tickets.Count(t => t.CreatedAt.Date == d)))
            .ToList();
    }
}
