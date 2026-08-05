using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace IForm.Tests;

public class DashboardControllerTests
{
    [Fact]
    public async Task Index_ComputesKpis()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var now = DateTime.UtcNow;
            db.Tickets.AddRange(
                TestData.Ticket(1, "TKT-0001", TicketStatus.Open, TicketPriority.Critical, user.Id, assignedToId: user.Id),
                TestData.Ticket(2, "TKT-0002", TicketStatus.Resolved, TicketPriority.Low, user.Id),
                TestData.Ticket(3, "TKT-0003", TicketStatus.Closed, TicketPriority.Medium, user.Id));
            db.Incidents.Add(new Incident
            {
                Id = 1,
                IncidentNumber = "INC-001",
                Title = "Test incident",
                Type = IncidentType.NearMiss,
                Severity = IncidentSeverity.Critical,
                Status = IncidentStatus.New,
                ReportedById = user.Id,
                OccurredAt = now,
                CreatedAt = now
            });
            db.ActionItems.Add(new ActionItem
            {
                Id = 1,
                ActionNumber = "ACT-001",
                Title = "Overdue action",
                Priority = ActionPriority.High,
                Status = ActionStatus.Open,
                AssignedToId = user.Id,
                CreatedById = user.Id,
                DueDate = now.AddDays(-3),
                CreatedAt = now
            });
            db.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Action = "Create",
                EntityType = "Ticket",
                EntityId = "TKT-0001",
                Details = "Seeded",
                CreatedAt = now
            });
            await db.SaveChangesAsync();

            var controller = new DashboardController(db, TestData.UserManager(user))
                .SetUser(TestData.Principal(user));

            var result = await controller.Index();

            var model = Assert.IsAssignableFrom<DashboardViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(3, model.TotalTickets);
            Assert.Equal(1, model.OpenTickets);
            Assert.Equal(1, model.CriticalTickets);
            Assert.Equal(1, model.MyAssignedTickets);
            Assert.Equal(1, model.TotalIncidents);
            Assert.Equal(1, model.OpenIncidents);
            Assert.Equal(1, model.OpenActions);
            Assert.Equal(1, model.OverdueActions);
            Assert.Single(model.RecentActivity);
            Assert.Equal(14, model.TicketsPerDay.Count);
            Assert.Equal(66.7, model.ResolvedRate, 1);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }
}
