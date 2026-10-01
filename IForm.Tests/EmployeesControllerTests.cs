using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Tests;

public class EmployeesControllerTests
{
    [Fact]
    public async Task Index_ReturnsAllEmployees_WithCounts()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            db.Employees.AddRange(
                TestData.Employee("EMP-001", "Asha", "Rao"),
                TestData.Employee("EMP-002", "Bala", "Kumar", status: EmploymentStatus.Active),
                TestData.Employee("EMP-003", "Chandra", "Iyer", status: EmploymentStatus.Exited));
            await db.SaveChangesAsync();

            var controller = new EmployeesController(db).SetUser(TestData.Principal(user, "Admin"));

            var model = Assert.IsAssignableFrom<EmployeeListViewModel>(
                Assert.IsType<ViewResult>(await controller.Index(null, null, null)).Model);

            Assert.Equal(3, model.TotalCount);
            Assert.Equal(2, model.ActiveCount);
            Assert.Equal(1, model.ExitedCount);
            Assert.Equal(3, model.Employees.Count);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Index_FiltersByDepartmentAndStatus()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var engineering = new Department { Code = "ENG", Name = "Engineering" };
            var quality = new Department { Code = "QA", Name = "Quality" };
            db.Departments.AddRange(engineering, quality);
            await db.SaveChangesAsync();

            db.Employees.AddRange(
                TestData.Employee("EMP-001", "Asha", "Rao", engineering.Id),
                TestData.Employee("EMP-002", "Bala", "Kumar", engineering.Id),
                TestData.Employee("EMP-003", "Chandra", "Iyer", quality.Id, status: EmploymentStatus.Exited));
            await db.SaveChangesAsync();

            var controller = new EmployeesController(db).SetUser(TestData.Principal(user, "Manager"));

            var inEngineering = Assert.IsAssignableFrom<EmployeeListViewModel>(
                Assert.IsType<ViewResult>(await controller.Index(null, engineering.Id, null)).Model);
            Assert.Equal(2, inEngineering.Employees.Count);

            var activeOnly = Assert.IsAssignableFrom<EmployeeListViewModel>(
                Assert.IsType<ViewResult>(await controller.Index(null, null, status: EmploymentStatus.Active)).Model);
            Assert.Equal(2, activeOnly.Employees.Count);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Index_SearchMatchesNameEmailOrCode()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            db.Employees.AddRange(
                TestData.Employee("EMP-001", "Asha", "Rao"),
                TestData.Employee("EMP-002", "Bala", "Kumar"),
                TestData.Employee("EMP-003", "Chandra", "Iyer"));
            await db.SaveChangesAsync();

            var controller = new EmployeesController(db).SetUser(TestData.Principal(user, "Admin"));

            var byName = Assert.IsAssignableFrom<EmployeeListViewModel>(
                Assert.IsType<ViewResult>(await controller.Index("Bala", null, null)).Model);
            Assert.Equal("EMP-002", Assert.Single(byName.Employees).EmployeeCode);

            var byEmailFragment = Assert.IsAssignableFrom<EmployeeListViewModel>(
                Assert.IsType<ViewResult>(await controller.Index("chandra.iyer", null, null)).Model);
            Assert.Equal("EMP-003", Assert.Single(byEmailFragment.Employees).EmployeeCode);

            var byCode = Assert.IsAssignableFrom<EmployeeListViewModel>(
                Assert.IsType<ViewResult>(await controller.Index("EMP-001", null, null)).Model);
            Assert.Equal("EMP-001", Assert.Single(byCode.Employees).EmployeeCode);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_PersistsEmployee_WithNormalisedCodeAndEmail()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var department = new Department { Code = "ENG", Name = "Engineering" };
            db.Departments.Add(department);
            await db.SaveChangesAsync();

            var controller = new EmployeesController(db).SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Create(new EmployeeFormViewModel
            {
                EmployeeCode = "  emp-101 ",
                FirstName = " Asha ",
                LastName = " Rao ",
                Email = "Asha.Rao@IForm.App",
                DepartmentId = department.Id,
                JobTitle = "Site Engineer",
                DateJoined = new DateTime(2026, 1, 15)
            });

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(EmployeesController.Index), redirect.ActionName);

            var saved = Assert.Single(db.Employees);
            Assert.Equal("EMP-101", saved.EmployeeCode);
            Assert.Equal("asha.rao@iform.app", saved.Email);
            Assert.Equal("Asha Rao", saved.FullName);
            Assert.Equal(EmploymentStatus.Active, saved.Status);
            Assert.Equal(department.Id, saved.DepartmentId);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_RejectsDuplicateEmployeeCode()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();

            var controller = new EmployeesController(db).SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Create(new EmployeeFormViewModel
            {
                EmployeeCode = "EMP-001",
                FirstName = "Bala",
                LastName = "Kumar",
                Email = "bala.kumar@iform.app"
            });

            Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ContainsKey(nameof(EmployeeFormViewModel.EmployeeCode)));
            Assert.Single(db.Employees);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_RejectsDuplicateEmailRegardlessOfCase()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();

            var controller = new EmployeesController(db).SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Create(new EmployeeFormViewModel
            {
                EmployeeCode = "EMP-999",
                FirstName = "Bala",
                LastName = "Kumar",
                Email = "asha.rao@iform.app"
            });

            Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ContainsKey(nameof(EmployeeFormViewModel.Email)));
            Assert.Single(db.Employees);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_RejectsManagerThatDoesNotExist()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var controller = new EmployeesController(db).SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Create(new EmployeeFormViewModel
            {
                EmployeeCode = "EMP-001",
                FirstName = "Asha",
                LastName = "Rao",
                Email = "asha.rao@iform.app",
                ManagerId = 4242
            });

            Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ContainsKey(nameof(EmployeeFormViewModel.ManagerId)));
            Assert.Empty(db.Employees);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Edit_UpdatesFields_AndPersists()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();

            var employee = await db.Employees.FirstAsync();
            var controller = new EmployeesController(db).SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Edit(new EmployeeFormViewModel
            {
                Id = employee.Id,
                EmployeeCode = "EMP-001",
                FirstName = "Asha",
                LastName = "Rao",
                Email = "asha.rao@iform.app",
                JobTitle = "Senior Site Engineer",
                Status = EmploymentStatus.NoticePeriod
            });

            Assert.IsType<RedirectToActionResult>(result);

            var saved = await db.Employees.FirstAsync();
            Assert.Equal("Senior Site Engineer", saved.JobTitle);
            Assert.Equal(EmploymentStatus.NoticePeriod, saved.Status);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Edit_RejectsSelfAsManager()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();

            var employee = await db.Employees.FirstAsync();
            var controller = new EmployeesController(db).SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Edit(new EmployeeFormViewModel
            {
                Id = employee.Id,
                EmployeeCode = "EMP-001",
                FirstName = "Asha",
                LastName = "Rao",
                Email = "asha.rao@iform.app",
                ManagerId = employee.Id
            });

            Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ContainsKey(nameof(EmployeeFormViewModel.ManagerId)));
            Assert.Null((await db.Employees.FirstAsync()).ManagerId);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Edit_RejectsManagerCycle()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            // Priya reports to Asha. Making Asha report to Priya would close a loop.
            db.Employees.AddRange(
                TestData.Employee("EMP-001", "Asha", "Rao"),
                TestData.Employee("EMP-002", "Priya", "Sharma"));
            await db.SaveChangesAsync();

            var asha = await db.Employees.FirstAsync(e => e.EmployeeCode == "EMP-001");
            var priya = await db.Employees.FirstAsync(e => e.EmployeeCode == "EMP-002");
            priya.ManagerId = asha.Id;
            await db.SaveChangesAsync();

            var controller = new EmployeesController(db).SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Edit(new EmployeeFormViewModel
            {
                Id = asha.Id,
                EmployeeCode = "EMP-001",
                FirstName = "Asha",
                LastName = "Rao",
                Email = "asha.rao@iform.app",
                ManagerId = priya.Id
            });

            Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ContainsKey(nameof(EmployeeFormViewModel.ManagerId)));
            Assert.Null((await db.Employees.FirstAsync(e => e.Id == asha.Id)).ManagerId);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Edit_RejectsLongerReportingCycle()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            // A -> B -> C. Pointing C at A would create a three-hop loop.
            db.Employees.AddRange(
                TestData.Employee("EMP-001", "A", "One"),
                TestData.Employee("EMP-002", "B", "Two"),
                TestData.Employee("EMP-003", "C", "Three"));
            await db.SaveChangesAsync();

            var a = await db.Employees.FirstAsync(e => e.EmployeeCode == "EMP-001");
            var b = await db.Employees.FirstAsync(e => e.EmployeeCode == "EMP-002");
            var c = await db.Employees.FirstAsync(e => e.EmployeeCode == "EMP-003");
            b.ManagerId = a.Id;
            c.ManagerId = b.Id;
            await db.SaveChangesAsync();

            var controller = new EmployeesController(db).SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Edit(new EmployeeFormViewModel
            {
                Id = a.Id,
                EmployeeCode = "EMP-001",
                FirstName = "A",
                LastName = "One",
                Email = a.Email,
                ManagerId = c.Id
            });

            Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ContainsKey(nameof(EmployeeFormViewModel.ManagerId)));
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Edit_MissingEmployee_ReturnsNotFound()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var controller = new EmployeesController(db).SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Edit(new EmployeeFormViewModel { Id = 999, EmployeeCode = "EMP-999", FirstName = "X", LastName = "Y", Email = "x.y@iform.app" });

            Assert.IsType<NotFoundResult>(result);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Details_ReturnsEmployee_AndDirectReports()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            db.Employees.AddRange(
                TestData.Employee("EMP-001", "Asha", "Rao"),
                TestData.Employee("EMP-002", "Bala", "Kumar"));
            await db.SaveChangesAsync();

            var manager = await db.Employees.FirstAsync(e => e.EmployeeCode == "EMP-001");
            var report = await db.Employees.FirstAsync(e => e.EmployeeCode == "EMP-002");
            report.ManagerId = manager.Id;
            await db.SaveChangesAsync();

            var controller = new EmployeesController(db).SetUser(TestData.Principal(user, "Manager"));

            var view = Assert.IsType<ViewResult>(await controller.Details(manager.Id));
            var employee = Assert.IsType<Employee>(view.Model);
            Assert.Equal("Asha Rao", employee.FullName);

            var reports = Assert.IsAssignableFrom<List<Employee>>(view.ViewData["DirectReports"]);
            Assert.Equal("EMP-002", Assert.Single(reports).EmployeeCode);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Details_MissingEmployee_ReturnsNotFound()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var controller = new EmployeesController(db).SetUser(TestData.Principal(user, "Admin"));

            Assert.IsType<NotFoundResult>(await controller.Details(999));
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task SetStatus_MarksEmployeeExited()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();

            var employee = await db.Employees.FirstAsync();
            var controller = new EmployeesController(db).SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.SetStatus(employee.Id, EmploymentStatus.Exited);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(EmploymentStatus.Exited, (await db.Employees.FirstAsync()).Status);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task SetStatus_MissingEmployee_ReturnsNotFound()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var controller = new EmployeesController(db).SetUser(TestData.Principal(user, "Admin"));

            Assert.IsType<NotFoundResult>(await controller.SetStatus(999, EmploymentStatus.Exited));
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_ExcludesInactiveDepartmentsFromDropdown()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            db.Departments.AddRange(
                TestData.Department("ENG", "Engineering"),
                TestData.Department("OLD", "Closed Department", isActive: false));
            await db.SaveChangesAsync();

            var controller = new EmployeesController(db).SetUser(TestData.Principal(user, "Admin"));

            var model = Assert.IsAssignableFrom<EmployeeFormViewModel>(
                Assert.IsType<ViewResult>(await controller.Create()).Model);

            Assert.Equal("ENG", Assert.Single(model.Departments).Code);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }
}