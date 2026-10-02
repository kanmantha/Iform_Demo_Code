using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Tests;

public class LmsControllerTests
{
    [Fact]
    public async Task Index_ReturnsCourses()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.LmsCourses.Add(new LmsCourse { Title = "Course", Status = LmsStatus.NotStarted });
            await db.SaveChangesAsync();
            var controller = new LmsController(db).SetUser(TestData.Principal(user, "Admin"));
            var result = await controller.Index(null, null);
            var model = Assert.IsAssignableFrom<LmsListViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Single(model.Courses);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }
}
