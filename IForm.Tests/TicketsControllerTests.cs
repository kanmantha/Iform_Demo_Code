using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace IForm.Tests;

public class TicketsControllerTests
{
    [Fact]
    public async Task Index_FiltersByStatusAndPriority()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            db.Tickets.AddRange(
                TestData.Ticket(1, "TKT-0001", TicketStatus.Open, TicketPriority.Critical, user.Id),
                TestData.Ticket(2, "TKT-0002", TicketStatus.Resolved, TicketPriority.Low, user.Id),
                TestData.Ticket(3, "TKT-0003", TicketStatus.Open, TicketPriority.Medium, user.Id));
            await db.SaveChangesAsync();

            var controller = new TicketsController(db, TestData.UserManager(user))
                .SetUser(TestData.Principal(user));

            var result = await controller.Index(search: null, status: TicketStatus.Open, priority: TicketPriority.Critical);

            var model = Assert.IsAssignableFrom<TicketListViewModel>(Assert.IsType<ViewResult>(result).Model);
            var only = Assert.Single(model.Tickets);
            Assert.Equal("TKT-0001", only.TicketNumber);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Board_GroupsTicketsByStatus()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            db.Tickets.AddRange(
                TestData.Ticket(1, "TKT-0001", TicketStatus.Open, TicketPriority.Critical, user.Id),
                TestData.Ticket(2, "TKT-0002", TicketStatus.Closed, TicketPriority.Low, user.Id),
                TestData.Ticket(3, "TKT-0003", TicketStatus.Open, TicketPriority.Medium, user.Id));
            await db.SaveChangesAsync();

            var controller = new TicketsController(db, TestData.UserManager(user))
                .SetUser(TestData.Principal(user));

            var result = await controller.Board(search: null, priority: null);

            var model = Assert.IsAssignableFrom<TicketBoardViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(6, model.Columns.Count);
            Assert.Equal(2, model.Columns.Single(c => c.Status == TicketStatus.Open).Tickets.Count);
            Assert.Single(model.Columns.Single(c => c.Status == TicketStatus.Closed).Tickets);
            Assert.Equal(3, model.TotalCount);
            Assert.False(model.CanMove);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Move_UpdatesStatus_AndReturnsCount()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Tickets.Add(TestData.Ticket(1, "TKT-0001", TicketStatus.Open, TicketPriority.High, user.Id, assignedToId: user.Id));
            await db.SaveChangesAsync();

            var controller = new TicketsController(db, TestData.UserManager(user))
                .SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Move(1, TicketStatus.InProgress);

            var json = Assert.IsType<JsonResult>(result);
            Assert.True(json.Get<bool>("ok"));
            Assert.Equal("InProgress", json.Get<string>("status"));

            var ticket = await db.Tickets.FindAsync(1);
            Assert.NotNull(ticket);
            Assert.Equal(TicketStatus.InProgress, ticket!.Status);
            Assert.Contains(db.AuditLogs, a => a.EntityType == "Ticket");
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Count_ReturnsOnlyAssignedOpenTickets()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            db.Tickets.AddRange(
                TestData.Ticket(1, "TKT-0001", TicketStatus.Open, TicketPriority.High, user.Id, assignedToId: user.Id),
                TestData.Ticket(2, "TKT-0002", TicketStatus.Closed, TicketPriority.High, user.Id, assignedToId: user.Id),
                TestData.Ticket(3, "TKT-0003", TicketStatus.Open, TicketPriority.High, user.Id));
            await db.SaveChangesAsync();

            var controller = new TicketsController(db, TestData.UserManager(user))
                .SetUser(TestData.Principal(user));

            var result = await controller.Count();

            var json = Assert.IsType<JsonResult>(result);
            Assert.Equal(1, json.Get<int>("count"));
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }
}
