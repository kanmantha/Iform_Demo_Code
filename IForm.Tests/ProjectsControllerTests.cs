using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Tests;

public class ProjectsControllerTests
{
    [Fact]
    public async Task Index_ReturnsProjects()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Projects.Add(new Project { Name = "Test", Status = ProjectStatus.Active });
            await db.SaveChangesAsync();

            var controller = new ProjectsController(db).SetUser(TestData.Principal(user, "Admin"));
            var result = await controller.Index(null, null);
            var model = Assert.IsAssignableFrom<ProjectListViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Single(model.Projects);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_PersistsProject()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            var controller = new ProjectsController(db).SetUser(TestData.Principal(user, "Manager"));
            var model = new ProjectFormViewModel { Name = "Test", Status = ProjectStatus.Active };
            var result = await controller.Create(model);
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(1, await db.Projects.CountAsync());
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }
}
