using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Tests;

public class TrainingControllerTests
{
    [Fact]
    public async Task Index_ReturnsTraining()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Trainings.Add(new Training { Title = "Course", Status = TrainingStatus.NotStarted, Type = TrainingType.Mandatory });
            await db.SaveChangesAsync();
            var controller = new TrainingController(db).SetUser(TestData.Principal(user, "Admin"));
            var result = await controller.Index(null, null, null);
            var model = Assert.IsAssignableFrom<TrainingListViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Single(model.Trainings);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }
}
