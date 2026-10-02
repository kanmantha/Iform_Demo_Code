using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Tests;

public class TimesheetsControllerTests
{
    [Fact]
    public async Task Index_ReturnsTimesheets()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var emp = await db.Employees.FirstAsync();

            db.Timesheets.Add(new Timesheet { EmployeeId = emp.Id, Date = DateTime.UtcNow.Date, Status = TimesheetStatus.Draft, Hours = 8 });
            await db.SaveChangesAsync();

            var controller = new TimesheetsController(db).SetUser(TestData.Principal(user, "Admin"));
            var result = await controller.Index(null, null, null, null, null);
            var model = Assert.IsAssignableFrom<TimesheetListViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Single(model.Timesheets);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_PersistsTimesheet()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var emp = await db.Employees.FirstAsync();

            var controller = new TimesheetsController(db).SetUser(TestData.Principal(user, "Admin"));
            var model = new TimesheetFormViewModel { EmployeeId = emp.Id, Date = DateTime.UtcNow.Date, Status = TimesheetStatus.Draft, Hours = 8 };
            var result = await controller.Create(model);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(1, await db.Timesheets.CountAsync());
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }
}
