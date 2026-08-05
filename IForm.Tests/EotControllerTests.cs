using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace IForm.Tests;

public class EotControllerTests
{
    [Fact]
    public async Task Index_ReturnsAllEots_WhenNoFilters()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            db.EotRequests.AddRange(
                TestData.Eot(1, "EOT-01", user.Id, EotCategory.DesignRevision),
                TestData.Eot(2, "EOT-02", user.Id, EotCategory.ScopeChange),
                TestData.Eot(3, "EOT-03", user.Id, EotCategory.ClientInstruction));
            await db.SaveChangesAsync();

            var controller = new EotController(db, TestData.UserManager(user))
                .SetUser(TestData.Principal(user));

            var result = await controller.Index();

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<EotListViewModel>(view.Model);
            Assert.Equal(3, model.TotalCount);
            Assert.Equal(3, model.EotRequests.Count);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Index_FiltersByCategoryAndSearch()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var e1 = TestData.Eot(1, "EOT-01", user.Id, EotCategory.ScopeChange);
            e1.Project = "Reliance E-1";
            db.EotRequests.AddRange(e1, TestData.Eot(2, "EOT-02", user.Id, EotCategory.DesignRevision));
            await db.SaveChangesAsync();

            var controller = new EotController(db, TestData.UserManager(user))
                .SetUser(TestData.Principal(user));

            var result = await controller.Index(category: EotCategory.ScopeChange);
            var model = Assert.IsAssignableFrom<EotListViewModel>(Assert.IsType<ViewResult>(result).Model);
            var only = Assert.Single(model.EotRequests);
            Assert.Equal("EOT-01", only.EotNumber);

            var searched = await controller.Index(search: "Reliance");
            var searchedModel = Assert.IsAssignableFrom<EotListViewModel>(Assert.IsType<ViewResult>(searched).Model);
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
            db.EotRequests.Add(TestData.Eot(5, "EOT-05", user.Id));
            await db.SaveChangesAsync();

            var controller = new EotController(db, TestData.UserManager(user))
                .SetUser(TestData.Principal(user));

            var result = await controller.Create(new EotFormViewModel
            {
                Project = "Golkonda Tattvam",
                Client = "Tattvam Constructions",
                Category = EotCategory.ApprovalDelay,
                Scenario = EotScenario.Sc3,
                Reason = "Consultant approval pending for revised slab edge detail.",
                FinancialYear = "2026-27",
                EstimatedTimeImpactDays = 10
            });

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(EotController.Details), redirect.ActionName);

            var saved = Assert.Single(db.EotRequests.Where(e => e.EotNumber == "EOT-06"));
            Assert.Equal("Golkonda Tattvam", saved.Project);
            Assert.Equal(EotCategory.ApprovalDelay, saved.Category);
            Assert.Equal(EotScenario.Sc3, saved.Scenario);
            Assert.Equal(10, saved.EstimatedTimeImpactDays);
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

            var controller = new EotController(db, TestData.UserManager(user))
                .SetUser(TestData.Principal(user));

            var model = new EotFormViewModel
            {
                Project = "",
                Category = EotCategory.DesignRevision,
                Reason = ""
            };
            controller.ModelState.AddModelError(nameof(model.Project), "Required");

            var result = await controller.Create(model);

            Assert.IsType<ViewResult>(result);
            Assert.Empty(db.EotRequests);
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
            db.EotRequests.Add(TestData.Eot(1, "EOT-01", user.Id));
            await db.SaveChangesAsync();

            var controller = new EotController(db, TestData.UserManager(user))
                .SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Edit(new EotFormViewModel
            {
                Id = 1,
                Project = "Updated Project",
                Client = "Updated Client",
                Category = EotCategory.SiteConstraint,
                Scenario = EotScenario.Sc1,
                Reason = "Updated reason.",
                SubmissionStatus = EotSubmissionStatus.Submitted,
                ClientApproval = EotClientApproval.UnderReview,
                HasApprovedDrawings = true,
                HasRevisedDrawings = true,
                HasClientInstructions = true,
                HasDelayAnalysis = true,
                HasScopeVariationStatement = true,
                HasProgressReport = true,
                HasConsultantCorrespondence = true
            });

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(1, redirect.RouteValues?["id"]);

            var saved = await db.EotRequests.FindAsync(1);
            Assert.NotNull(saved);
            Assert.Equal("Updated Project", saved.Project);
            Assert.Equal(EotCategory.SiteConstraint, saved.Category);
            Assert.Equal(EotSubmissionStatus.Submitted, saved.SubmissionStatus);
            Assert.True(EotDocuments.IsComplete(saved));
            Assert.NotNull(saved.UpdatedAt);
            Assert.Contains(db.AuditLogs, a => a.EntityType == "EotRequest");
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Edit_MissingEot_ReturnsNotFound()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var controller = new EotController(db, TestData.UserManager(user))
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
}
