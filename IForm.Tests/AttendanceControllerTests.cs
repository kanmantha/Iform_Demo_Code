using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Tests;

public class AttendanceControllerTests
{
    private static readonly DateTime WorkDay = new(2026, 3, 2);

    [Fact]
    public async Task Create_DerivesHoursAndPresentStatus()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            var controller = new AttendanceController(db).SetUser(TestData.Principal(user, "Manager"));

            var result = await controller.Create(new AttendanceFormViewModel
            {
                EmployeeId = employeeId,
                Date = WorkDay,
                CheckIn = WorkDay.AddHours(9),
                CheckOut = WorkDay.AddHours(18).AddMinutes(30)
            });

            Assert.IsType<RedirectToActionResult>(result);

            var saved = Assert.Single(db.AttendanceRecords);
            Assert.Equal(9.5m, saved.HoursWorked);
            Assert.Equal(AttendanceStatus.Present, saved.Status);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_HalfDayForShortShift()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            var controller = new AttendanceController(db).SetUser(TestData.Principal(user, "Admin"));

            await controller.Create(new AttendanceFormViewModel
            {
                EmployeeId = employeeId,
                Date = WorkDay,
                CheckIn = WorkDay.AddHours(9),
                CheckOut = WorkDay.AddHours(13)
            });

            var saved = Assert.Single(db.AttendanceRecords);
            Assert.Equal(4m, saved.HoursWorked);
            Assert.Equal(AttendanceStatus.HalfDay, saved.Status);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_NoTimesRecordsAbsence()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            var controller = new AttendanceController(db).SetUser(TestData.Principal(user, "Manager"));

            await controller.Create(new AttendanceFormViewModel
            {
                EmployeeId = employeeId,
                Date = WorkDay,
                Notes = "Absent without notice"
            });

            var saved = Assert.Single(db.AttendanceRecords);
            Assert.Equal(0m, saved.HoursWorked);
            Assert.Equal(AttendanceStatus.Absent, saved.Status);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_RejectsDuplicateEmployeeDate()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            db.AttendanceRecords.Add(new AttendanceRecord
            {
                EmployeeId = employeeId,
                Date = WorkDay,
                HoursWorked = 8m,
                Status = AttendanceStatus.Present
            });
            await db.SaveChangesAsync();

            var controller = new AttendanceController(db).SetUser(TestData.Principal(user, "Manager"));

            var result = await controller.Create(new AttendanceFormViewModel
            {
                EmployeeId = employeeId,
                Date = WorkDay,
                CheckIn = WorkDay.AddHours(9),
                CheckOut = WorkDay.AddHours(17)
            });

            Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ContainsKey(nameof(AttendanceFormViewModel.Date)));
            Assert.Single(db.AttendanceRecords);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_RejectsCheckoutBeforeCheckin()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            var controller = new AttendanceController(db).SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Create(new AttendanceFormViewModel
            {
                EmployeeId = employeeId,
                Date = WorkDay,
                CheckIn = WorkDay.AddHours(18),
                CheckOut = WorkDay.AddHours(9)
            });

            Assert.IsType<ViewResult>(result);
            Assert.Empty(db.AttendanceRecords);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_RejectsOnlyOneTimestampProvided()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            var controller = new AttendanceController(db).SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Create(new AttendanceFormViewModel
            {
                EmployeeId = employeeId,
                Date = WorkDay,
                CheckIn = WorkDay.AddHours(9)
            });

            Assert.IsType<ViewResult>(result);
            Assert.Empty(db.AttendanceRecords);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_RejectsShiftLongerThanDailyMaximum()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            var controller = new AttendanceController(db).SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Create(new AttendanceFormViewModel
            {
                EmployeeId = employeeId,
                Date = WorkDay,
                CheckIn = WorkDay.AddHours(6),
                CheckOut = WorkDay.AddHours(23)
            });

            Assert.IsType<ViewResult>(result);
            Assert.Empty(db.AttendanceRecords);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_IgnoresEnteredHoursAndUsesTimestamps()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            var controller = new AttendanceController(db).SetUser(TestData.Principal(user, "Manager"));

            // A client sending an inflated HoursWorked must not be able to store it.
            await controller.Create(new AttendanceFormViewModel
            {
                EmployeeId = employeeId,
                Date = WorkDay,
                CheckIn = WorkDay.AddHours(9),
                CheckOut = WorkDay.AddHours(14),
                HoursWorked = 12m
            });

            Assert.Equal(5m, Assert.Single(db.AttendanceRecords).HoursWorked);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Index_SumsHours_AndFiltersByDateRange()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            db.AttendanceRecords.AddRange(
                new AttendanceRecord { EmployeeId = employeeId, Date = WorkDay, HoursWorked = 8m, Status = AttendanceStatus.Present },
                new AttendanceRecord { EmployeeId = employeeId, Date = WorkDay.AddDays(1), HoursWorked = 7.5m, Status = AttendanceStatus.Present },
                new AttendanceRecord { EmployeeId = employeeId, Date = WorkDay.AddDays(20), HoursWorked = 6m, Status = AttendanceStatus.Present });
            await db.SaveChangesAsync();

            var controller = new AttendanceController(db).SetUser(TestData.Principal(user, "Manager"));

            var all = Assert.IsAssignableFrom<AttendanceListViewModel>(
                Assert.IsType<ViewResult>(await controller.Index(null, null, null, null)).Model);
            Assert.Equal(3, all.TotalCount);
            Assert.Equal(21.5m, all.TotalHours);

            var ranged = Assert.IsAssignableFrom<AttendanceListViewModel>(
                Assert.IsType<ViewResult>(await controller.Index(WorkDay, WorkDay.AddDays(1), null, null)).Model);
            Assert.Equal(2, ranged.TotalCount);
            Assert.Equal(15.5m, ranged.TotalHours);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Index_FiltersByStatus()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            db.AttendanceRecords.AddRange(
                new AttendanceRecord { EmployeeId = employeeId, Date = WorkDay, HoursWorked = 8m, Status = AttendanceStatus.Present },
                new AttendanceRecord { EmployeeId = employeeId, Date = WorkDay.AddDays(1), HoursWorked = 0m, Status = AttendanceStatus.Absent });
            await db.SaveChangesAsync();

            var controller = new AttendanceController(db).SetUser(TestData.Principal(user, "Admin"));

            var model = Assert.IsAssignableFrom<AttendanceListViewModel>(
                Assert.IsType<ViewResult>(await controller.Index(null, null, null, AttendanceStatus.Absent)).Model);

            Assert.Equal(AttendanceStatus.Absent, Assert.Single(model.Records).Status);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Delete_RemovesRecord()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            db.AttendanceRecords.Add(new AttendanceRecord { EmployeeId = employeeId, Date = WorkDay, HoursWorked = 8m, Status = AttendanceStatus.Present });
            await db.SaveChangesAsync();

            var record = await db.AttendanceRecords.FirstAsync();
            var controller = new AttendanceController(db).SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Delete(record.Id);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Empty(db.AttendanceRecords);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Delete_MissingRecord_ReturnsNotFound()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var controller = new AttendanceController(db).SetUser(TestData.Principal(user, "Admin"));

            Assert.IsType<NotFoundResult>(await controller.Delete(999));
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }
}