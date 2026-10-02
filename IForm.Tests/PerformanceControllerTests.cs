using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Tests;

public class PerformanceControllerTests
{
    [Fact]
    public async Task Index_ReturnsReviews()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var emp = await db.Employees.FirstAsync();
            db.PerformanceReviews.Add(new PerformanceReview { EmployeeId = emp.Id, Status = PerformanceStatus.Draft, Rating = PerformanceRating.MeetsExpectations });
            await db.SaveChangesAsync();
            var controller = new PerformanceController(db).SetUser(TestData.Principal(user, "Admin"));
            var result = await controller.Index(null, null, null);
            var model = Assert.IsAssignableFrom<PerformanceListViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Single(model.Reviews);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }
}
