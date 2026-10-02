using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Tests;

public class EngagementControllerTests
{
    [Fact]
    public async Task Index_ReturnsSurveys()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.EngagementSurveys.Add(new EngagementSurvey { Title = "Survey", Status = EngagementStatus.Open, StartDate = DateTime.UtcNow });
            await db.SaveChangesAsync();
            var controller = new EngagementController(db).SetUser(TestData.Principal(user, "Admin"));
            var result = await controller.Index(null, null);
            var model = Assert.IsAssignableFrom<EngagementListViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Single(model.Surveys);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }
}
