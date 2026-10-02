using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Tests;

public class MyPayslipsControllerTests
{
    [Fact]
    public async Task Index_ReturnsOwnPayslips()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            var empEntity = TestData.Employee("EMP-001", "Asha", "Rao");
            empEntity.AppUserId = user.Id;
            db.Employees.Add(empEntity);
            await db.SaveChangesAsync();
            var emp = await db.Employees.FirstAsync();
            db.Payslips.Add(new Payslip { EmployeeId = emp.Id, Year = 2026, Month = "10", Status = PayslipStatus.Paid });
            await db.SaveChangesAsync();
            var controller = new MyPayslipsController(db, TestData.UserManager(user)).SetUser(TestData.Principal(user));
            var result = await controller.Index(null, null, null);
            var model = Assert.IsAssignableFrom<MyPayslipListViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Single(model.Payslips);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }
}
