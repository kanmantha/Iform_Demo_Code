using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace IForm.Tests;

public class DispatchControllerTests
{
    [Fact]
    public async Task Index_ReturnsAllEntries_WhenNoFilters()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            db.DispatchOrders.AddRange(
                TestData.Dispatch(1, "DT-01", "Hallmark", user.Id, DispatchStatus.Pending),
                TestData.Dispatch(2, "DT-02", "Reliance E-1", user.Id, DispatchStatus.Dispatched),
                TestData.Dispatch(3, "DT-03", "Golkonda Tattvam", user.Id, DispatchStatus.Pending));
            await db.SaveChangesAsync();

            var controller = new DispatchController(db, TestData.UserManager(user))
                .SetUser(TestData.Principal(user));

            var result = await controller.Index();

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<DispatchListViewModel>(view.Model);
            Assert.Equal(3, model.TotalCount);
            Assert.Equal(2, model.PendingCount);
            Assert.Equal(3, model.DispatchOrders.Count);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Index_FiltersByStatusAndSearch()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            db.DispatchOrders.AddRange(
                TestData.Dispatch(1, "DT-01", "Hallmark", user.Id, DispatchStatus.Pending),
                TestData.Dispatch(2, "DT-02", "Reliance E-1", user.Id, DispatchStatus.Dispatched));
            await db.SaveChangesAsync();

            var controller = new DispatchController(db, TestData.UserManager(user))
                .SetUser(TestData.Principal(user));

            var pending = Assert.IsAssignableFrom<DispatchListViewModel>(
                Assert.IsType<ViewResult>(await controller.Index(status: DispatchStatus.Dispatched)).Model);
            var only = Assert.Single(pending.DispatchOrders);
            Assert.Equal("DT-02", only.DispatchNumber);

            var searched = Assert.IsAssignableFrom<DispatchListViewModel>(
                Assert.IsType<ViewResult>(await controller.Index(search: "Reliance")).Model);
            Assert.Equal(1, searched.TotalCount);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_GeneratesSequentialNumber_AndPersists()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.DispatchOrders.Add(TestData.Dispatch(5, "DT-05", "Hallmark", user.Id));
            await db.SaveChangesAsync();

            var controller = new DispatchController(db, TestData.UserManager(user))
                .SetUser(TestData.Principal(user, "Manager"));

            var result = await controller.Create(new DispatchFormViewModel
            {
                IpoNumber = "IPO-777",
                Project = "Golkonda Tattvam",
                QuantityNos = 18,
                QuantitySqm = 9.25m,
                DispatchStatus = DispatchStatus.Pending,
                DelayDays = 12,
                Reason = QueryCategory.ProductionMistake
            });

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(DispatchController.Details), redirect.ActionName);

            var saved = Assert.Single(db.DispatchOrders.Where(d => d.DispatchNumber == "DT-06"));
            Assert.Equal("IPO-777", saved.IpoNumber);
            Assert.Equal("Golkonda Tattvam", saved.Project);
            Assert.Equal(18, saved.QuantityNos);
            Assert.Equal(QueryCategory.ProductionMistake, saved.Reason);
            Assert.Single(db.AuditLogs);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_InvalidModel_ReturnsView_AndDoesNotPersist()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var controller = new DispatchController(db, TestData.UserManager(user))
                .SetUser(TestData.Principal(user, "Manager"));

            var model = new DispatchFormViewModel { IpoNumber = "", Project = "" };
            controller.ModelState.AddModelError(nameof(model.Project), "Required");

            var result = await controller.Create(model);

            Assert.IsType<ViewResult>(result);
            Assert.Empty(db.DispatchOrders);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Edit_UpdatesFields_AndWritesAudit()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.DispatchOrders.Add(TestData.Dispatch(1, "DT-01", "Hallmark", user.Id));
            await db.SaveChangesAsync();

            var controller = new DispatchController(db, TestData.UserManager(user))
                .SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Edit(new DispatchFormViewModel
            {
                Id = 1,
                IpoNumber = "IPO-888",
                Project = "Updated Project",
                DispatchStatus = DispatchStatus.Dispatched,
                DispatchDate = DateTime.UtcNow.AddDays(-2),
                DelayDays = 3,
                Reason = QueryCategory.DesignMistake
            });

            Assert.IsType<RedirectToActionResult>(result);

            var saved = await db.DispatchOrders.FindAsync(1);
            Assert.NotNull(saved);
            Assert.Equal("IPO-888", saved.IpoNumber);
            Assert.Equal("Updated Project", saved.Project);
            Assert.Equal(DispatchStatus.Dispatched, saved.DispatchStatus);
            Assert.Equal(QueryCategory.DesignMistake, saved.Reason);
            Assert.NotNull(saved.UpdatedAt);
            Assert.Contains(db.AuditLogs, a => a.EntityType == "DispatchOrder");
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Details_MissingEntry_ReturnsNotFound()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var controller = new DispatchController(db, TestData.UserManager(user))
                .SetUser(TestData.Principal(user));

            var result = await controller.Details(999);

            Assert.IsType<NotFoundResult>(result);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }
}
