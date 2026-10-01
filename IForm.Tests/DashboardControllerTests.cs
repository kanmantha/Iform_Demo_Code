using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace IForm.Tests;

public class DashboardControllerTests
{
    [Fact]
    public async Task Index_ComputesKpis_ForCurrentUser()
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
            Assert.Equal(3, model.MyReportedTickets);
            Assert.Equal(1, model.MyOpenTickets);
            Assert.Equal(1, model.MyCriticalTickets);
            Assert.Equal(1, model.MyAssignedTickets);
            Assert.Equal(1, model.MyIncidents);
            Assert.Equal(1, model.MyOpenIncidents);
            Assert.Equal(1, model.MyOpenActions);
            Assert.Equal(1, model.MyOverdueActions);
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

    [Fact]
    public async Task Index_ExcludesOtherUsersData()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            var other = TestData.User("u2", "Other User", "other@iform.app");
            await TestData.SeedUsersAsync(db, user, other);

            var now = DateTime.UtcNow;
            db.Tickets.AddRange(
                TestData.Ticket(1, "TKT-0001", TicketStatus.Open, TicketPriority.Critical, user.Id),
                TestData.Ticket(2, "TKT-0002", TicketStatus.Resolved, TicketPriority.Low, other.Id),
                TestData.Ticket(3, "TKT-0003", TicketStatus.Closed, TicketPriority.Medium, other.Id, assignedToId: user.Id));
            db.Incidents.Add(new Incident
            {
                Id = 1,
                IncidentNumber = "INC-001",
                Title = "Other user's incident",
                Type = IncidentType.NearMiss,
                Severity = IncidentSeverity.Critical,
                Status = IncidentStatus.New,
                ReportedById = other.Id,
                OccurredAt = now,
                CreatedAt = now
            });
            db.ActionItems.Add(new ActionItem
            {
                Id = 1,
                ActionNumber = "ACT-001",
                Title = "Other user's action",
                Priority = ActionPriority.High,
                Status = ActionStatus.Open,
                AssignedToId = other.Id,
                CreatedById = other.Id,
                DueDate = now.AddDays(-3),
                CreatedAt = now
            });
            db.AuditLogs.Add(new AuditLog
            {
                UserId = other.Id,
                UserName = other.FullName,
                Action = "Create",
                EntityType = "Ticket",
                EntityId = "TKT-0002",
                Details = "Other user activity",
                CreatedAt = now
            });
            await db.SaveChangesAsync();

            var controller = new DashboardController(db, TestData.UserManager(user))
                .SetUser(TestData.Principal(user));

            var result = await controller.Index();

            var model = Assert.IsAssignableFrom<DashboardViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(1, model.MyReportedTickets);
            Assert.Equal(2, model.RecentTickets.Count);
            Assert.Equal(0, model.MyAssignedTickets);
            Assert.Equal(0, model.MyIncidents);
            Assert.Equal(0, model.MyOpenActions);
            Assert.Empty(model.RecentActivity);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }
}
