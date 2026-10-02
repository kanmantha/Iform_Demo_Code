using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Tests;

public class DirectoryControllerTests
{
    [Fact]
    public async Task Index_ReturnsEntries()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var emp = await db.Employees.FirstAsync();
            db.DirectoryEntries.Add(new DirectoryEntry { EmployeeId = emp.Id, IsPublished = true });
            await db.SaveChangesAsync();
            var controller = new DirectoryController(db).SetUser(TestData.Principal(user));
            var result = await controller.Index(null);
            var model = Assert.IsAssignableFrom<DirectoryListViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Single(model.Entries);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }
}
