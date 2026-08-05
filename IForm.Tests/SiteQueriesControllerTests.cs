using System.IO;
using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IForm.Tests;

public class SiteQueriesControllerTests
{
    private static IFormFile TestPhoto()
        => new FormFile(new MemoryStream(new byte[] { 1, 2, 3, 4 }), 0, 4, "photo", "photo.jpg");

    [Fact]
    public async Task Index_ReturnsAllQueries_WhenNoFilters()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            db.SiteQueries.AddRange(
                TestData.SiteQuery(1, "QY-001", QueryStatus.Pending, user.Id),
                TestData.SiteQuery(2, "QY-002", QueryStatus.Resolved, user.Id),
                TestData.SiteQuery(3, "QY-003", QueryStatus.InProgress, user.Id));
            await db.SaveChangesAsync();

            var controller = new SiteQueriesController(db, TestData.UserManager(user), new TestWebHostEnvironment())
                .SetUser(TestData.Principal(user));

            var result = await controller.Index(search: null);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<SiteQueryListViewModel>(view.Model);
            Assert.Equal(3, model.TotalCount);
            Assert.Equal(3, model.SiteQueries.Count);
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

            var q1 = TestData.SiteQuery(1, "QY-001", QueryStatus.Pending, user.Id);
            q1.IpoNumber = "556";
            q1.Description = "Approved BOQ item not delivered.";
            var q2 = TestData.SiteQuery(2, "QY-002", QueryStatus.Resolved, user.Id);
            q2.IpoNumber = "999";
            db.SiteQueries.AddRange(q1, q2);
            await db.SaveChangesAsync();

            var controller = new SiteQueriesController(db, TestData.UserManager(user), new TestWebHostEnvironment())
                .SetUser(TestData.Principal(user));

            var result = await controller.Index(search: null, project: null, category: null, status: QueryStatus.Pending);

            var model = Assert.IsAssignableFrom<SiteQueryListViewModel>(Assert.IsType<ViewResult>(result).Model);
            var only = Assert.Single(model.SiteQueries);
            Assert.Equal("QY-001", only.QueryNumber);

            var searched = await controller.Index(search: "556");
            var searchedModel = Assert.IsAssignableFrom<SiteQueryListViewModel>(Assert.IsType<ViewResult>(searched).Model);
            Assert.Equal(1, searchedModel.TotalCount);
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
            db.SiteQueries.Add(TestData.SiteQuery(7, "QY-007", QueryStatus.Pending, user.Id));
            await db.SaveChangesAsync();

            var controller = new SiteQueriesController(db, TestData.UserManager(user), new TestWebHostEnvironment())
                .SetUser(TestData.Principal(user));

            var result = await controller.Create(new SiteQueryFormViewModel
            {
                IpoNumber = "556",
                Project = "Hallmark",
                Category = QueryCategory.Missing,
                Description = "Missing mullion profiles.",
                QuantityNos = 10,
                QuantitySqm = 3.5m,
                Photo = TestPhoto()
            });

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(SiteQueriesController.Details), redirect.ActionName);

            var saved = Assert.Single(db.SiteQueries.Where(q => q.QueryNumber == "QY-008"));
            Assert.Equal("556", saved.IpoNumber);
            Assert.Equal("Hallmark", saved.Project);
            Assert.Equal(QueryStatus.Pending, saved.Status);
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

            var controller = new SiteQueriesController(db, TestData.UserManager(user), new TestWebHostEnvironment())
                .SetUser(TestData.Principal(user));

            var model = new SiteQueryFormViewModel
            {
                IpoNumber = "",
                Project = "Hallmark",
                Description = "Missing item."
            };
            controller.ModelState.AddModelError(nameof(model.IpoNumber), "Required");

            var result = await controller.Create(model);

            Assert.IsType<ViewResult>(result);
            Assert.Empty(db.SiteQueries);
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
            db.SiteQueries.Add(TestData.SiteQuery(1, "QY-001", QueryStatus.Pending, user.Id));
            await db.SaveChangesAsync();

            var controller = new SiteQueriesController(db, TestData.UserManager(user), new TestWebHostEnvironment())
                .SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Edit(new SiteQueryFormViewModel
            {
                Id = 1,
                IpoNumber = "556",
                Project = "Updated Project",
                Category = QueryCategory.DesignMistake,
                Description = "Updated description.",
                QuantityNos = 7,
                QuantitySqm = 1.25m
            });

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(1, redirect.RouteValues?["id"]);

            var saved = await db.SiteQueries.FindAsync(1);
            Assert.NotNull(saved);
            Assert.Equal("Updated Project", saved.Project);
            Assert.Equal(QueryCategory.DesignMistake, saved.Category);
            Assert.Equal(7m, saved.QuantityNos);
            Assert.NotNull(saved.UpdatedAt);
            Assert.Contains(db.AuditLogs, a => a.EntityType == "SiteQuery");
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Edit_MissingQuery_ReturnsNotFound()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var controller = new SiteQueriesController(db, TestData.UserManager(user), new TestWebHostEnvironment())
                .SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Edit(999);

            Assert.IsType<NotFoundResult>(result);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task ChangeStatus_ResolvingNotifiesCreator()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var engineer = TestData.User("u1", "John Carter", "john@iform.app");
            var manager = TestData.User("u2", "Sarah Mitchell", "sarah@iform.app");
            await TestData.SeedUsersAsync(db, engineer, manager);

            db.SiteQueries.Add(TestData.SiteQuery(1, "QY-001", QueryStatus.Pending, engineer.Id));
            await db.SaveChangesAsync();

            var controller = new SiteQueriesController(db, TestData.UserManager(manager), new TestWebHostEnvironment())
                .SetUser(TestData.Principal(manager, "Manager"));

            var result = await controller.ChangeStatus(1, QueryStatus.Resolved, null);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(SiteQueriesController.Details), redirect.ActionName);

            var saved = await db.SiteQueries.FindAsync(1);
            Assert.NotNull(saved);
            Assert.Equal(QueryStatus.Resolved, saved.Status);
            Assert.Equal(manager.Id, saved.ResolvedById);
            Assert.NotNull(saved.ResolvedAt);

            var notification = Assert.Single(db.Notifications);
            Assert.Equal(engineer.Id, notification.UserId);
            Assert.Equal(1, notification.SiteQueryId);
            Assert.False(notification.IsRead);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task ChangeStatus_ResolvingOwnQuery_DoesNotNotify()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var engineer = TestData.User();
            await TestData.SeedUsersAsync(db, engineer);

            db.SiteQueries.Add(TestData.SiteQuery(1, "QY-001", QueryStatus.Pending, engineer.Id));
            await db.SaveChangesAsync();

            var controller = new SiteQueriesController(db, TestData.UserManager(engineer), new TestWebHostEnvironment())
                .SetUser(TestData.Principal(engineer, "Manager"));

            var result = await controller.ChangeStatus(1, QueryStatus.Resolved, null);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Empty(db.Notifications);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Index_EscalatedFilter_ReturnsOnlyStaleOpenQueries()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            db.SiteQueries.AddRange(
                TestData.SiteQuery(40, "QY-040", QueryStatus.Pending, user.Id),
                TestData.SiteQuery(10, "QY-010", QueryStatus.Pending, user.Id),
                TestData.SiteQuery(50, "QY-040R", QueryStatus.Resolved, user.Id));
            await db.SaveChangesAsync();

            var controller = new SiteQueriesController(db, TestData.UserManager(user), new TestWebHostEnvironment())
                .SetUser(TestData.Principal(user));

            var result = await controller.Index(escalated: true);

            var model = Assert.IsAssignableFrom<SiteQueryListViewModel>(Assert.IsType<ViewResult>(result).Model);
            var only = Assert.Single(model.SiteQueries);
            Assert.Equal("QY-040", only.QueryNumber);
            Assert.True(model.Escalated);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Dashboard_CountsEscalatedQueries()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            db.SiteQueries.AddRange(
                TestData.SiteQuery(40, "QY-040", QueryStatus.Pending, user.Id),
                TestData.SiteQuery(5, "QY-005", QueryStatus.Pending, user.Id),
                TestData.SiteQuery(60, "QY-060", QueryStatus.Resolved, user.Id));
            await db.SaveChangesAsync();

            var controller = new SiteQueriesController(db, TestData.UserManager(user), new TestWebHostEnvironment())
                .SetUser(TestData.Principal(user, "Manager"));

            var result = await controller.Dashboard();

            var model = Assert.IsAssignableFrom<SiteQueryDashboardViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(2, model.OpenQueries);
            Assert.Equal(1, model.EscalatedQueries);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public void Create_RequiresPhotoAndQuantities_PerBrd()
    {
        var model = new SiteQueryFormViewModel
        {
            IpoNumber = "556",
            Project = "Hallmark",
            Category = QueryCategory.Missing,
            Description = "Missing item.",
            QuantityNos = 0,
            QuantitySqm = 0,
            Photo = null
        };

        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            model,
            new System.ComponentModel.DataAnnotations.ValidationContext(model),
            results,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SiteQueryFormViewModel.Photo)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SiteQueryFormViewModel.QuantityNos)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SiteQueryFormViewModel.QuantitySqm)));
    }

    [Fact]
    public async Task Dashboard_BreakdownCountsOnlyOpenQueries()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            db.SiteQueries.AddRange(
                TestData.SiteQuery(1, "QY-001", QueryStatus.Pending, user.Id, QueryCategory.Missing),
                TestData.SiteQuery(2, "QY-002", QueryStatus.InProgress, user.Id, QueryCategory.DesignMistake),
                TestData.SiteQuery(3, "QY-003", QueryStatus.Resolved, user.Id, QueryCategory.ProductionMistake));
            await db.SaveChangesAsync();

            var controller = new SiteQueriesController(db, TestData.UserManager(user), new TestWebHostEnvironment())
                .SetUser(TestData.Principal(user, "Manager"));

            var result = await controller.Dashboard();

            var model = Assert.IsAssignableFrom<SiteQueryDashboardViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal(2, model.OpenQueries);
            Assert.Equal(1, model.ByCategory[QueryCategory.Missing.ToString()]);
            Assert.Equal(1, model.ByCategory[QueryCategory.DesignMistake.ToString()]);
            Assert.DoesNotContain(QueryCategory.ProductionMistake.ToString(), model.ByCategory.Keys);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task AddComment_OnResolvedQuery_IsBlocked()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            db.SiteQueries.Add(TestData.SiteQuery(1, "QY-001", QueryStatus.Resolved, user.Id));
            await db.SaveChangesAsync();

            var controller = new SiteQueriesController(db, TestData.UserManager(user), new TestWebHostEnvironment())
                .SetUser(TestData.Principal(user));

            var result = await controller.AddComment(1, "Trying to add a comment to a closed query.");

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Empty(db.QueryComments);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }
}
