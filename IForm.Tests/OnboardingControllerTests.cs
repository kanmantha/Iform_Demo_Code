using IForm.Web.Controllers;
using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Tests;

public class OnboardingControllerTests
{
    private static async Task<(OnboardingTemplate Template, Employee Employee)> SeedTemplateAsync(
        ApplicationDbContext db, string name = "Site Engineer", params (string Title, int DueDayOffset)[] tasks)
    {
        db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
        await db.SaveChangesAsync();
        var employee = await db.Employees.FirstAsync();

        var template = new OnboardingTemplate { Name = name, Description = "Standard checklist", CreatedAt = DateTime.UtcNow };
        var order = 1;
        foreach (var (title, offset) in tasks)
        {
            template.TaskTemplates.Add(new OnboardingTaskTemplate { Title = title, DueDayOffset = offset, DisplayOrder = order++ });
        }

        db.OnboardingTemplates.Add(template);
        await db.SaveChangesAsync();
        return (template, employee);
    }

    [Fact]
    public async Task Start_CopiesTemplateTasks_WithDueDates()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            var (template, employee) = await SeedTemplateAsync(db, "Site Engineer",
                ("Collect ID proof", 1),
                ("Safety induction", 3),
                ("Issue PPE", 7));

            var controller = new OnboardingController(db).SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Start(new OnboardingStartFormViewModel
            {
                EmployeeId = employee.Id,
                OnboardingTemplateId = template.Id
            });

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(OnboardingController.Details), redirect.ActionName);

            var onboarding = await db.EmployeeOnboardings.Include(o => o.Tasks).FirstAsync();
            Assert.Equal(3, onboarding.Tasks.Count);
            Assert.Equal(employee.Id, onboarding.EmployeeId);
            Assert.Null(onboarding.CompletedAt);

            var start = onboarding.StartedAt.Date;
            Assert.All(onboarding.Tasks, t => Assert.True(t.DueDate.Date >= start));
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Start_RejectsDuplicateChecklistForSameEmployeeAndTemplate()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            var (template, employee) = await SeedTemplateAsync(db, "Site Engineer", ("Collect ID proof", 1));

            var controller = new OnboardingController(db).SetUser(TestData.Principal(user, "Admin"));
            await controller.Start(new OnboardingStartFormViewModel { EmployeeId = employee.Id, OnboardingTemplateId = template.Id });

            var second = await controller.Start(new OnboardingStartFormViewModel { EmployeeId = employee.Id, OnboardingTemplateId = template.Id });

            Assert.IsType<ViewResult>(second);
            Assert.Single(db.EmployeeOnboardings);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Start_RejectsUnknownTemplate()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employee = await db.Employees.FirstAsync();

            var controller = new OnboardingController(db).SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Start(new OnboardingStartFormViewModel { EmployeeId = employee.Id, OnboardingTemplateId = 999 });

            Assert.IsType<ViewResult>(result);
            Assert.Empty(db.EmployeeOnboardings);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task ToggleTask_CompletesTaskAndRecordsWhoDidIt()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            var (template, employee) = await SeedTemplateAsync(db, "Site Engineer", ("Collect ID proof", 1), ("Safety induction", 3));

            var controller = new OnboardingController(db).SetUser(TestData.Principal(user, "Manager"));
            await controller.Start(new OnboardingStartFormViewModel { EmployeeId = employee.Id, OnboardingTemplateId = template.Id });

            var task = await db.EmployeeOnboardingTasks.FirstAsync();
            await controller.ToggleTask(task.Id);

            var saved = await db.EmployeeOnboardingTasks.FindAsync(task.Id);
            Assert.True(saved!.IsCompleted);
            Assert.NotNull(saved.CompletedAt);
            Assert.Equal(user.Id, saved.CompletedById);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task ToggleTask_ReopensTaskAndClearsCompletion()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            var (template, employee) = await SeedTemplateAsync(db, "Site Engineer", ("Collect ID proof", 1));

            var controller = new OnboardingController(db).SetUser(TestData.Principal(user, "Admin"));
            await controller.Start(new OnboardingStartFormViewModel { EmployeeId = employee.Id, OnboardingTemplateId = template.Id });

            var task = await db.EmployeeOnboardingTasks.FirstAsync();
            await controller.ToggleTask(task.Id);
            await controller.ToggleTask(task.Id);

            var saved = await db.EmployeeOnboardingTasks.FindAsync(task.Id);
            Assert.False(saved!.IsCompleted);
            Assert.Null(saved.CompletedAt);
            Assert.Null(saved.CompletedById);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task ToggleTask_CompletesOnboarding_WhenAllTasksDone()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            var (template, employee) = await SeedTemplateAsync(db, "Site Engineer", ("Collect ID proof", 1), ("Safety induction", 3));

            var controller = new OnboardingController(db).SetUser(TestData.Principal(user, "Admin"));
            await controller.Start(new OnboardingStartFormViewModel { EmployeeId = employee.Id, OnboardingTemplateId = template.Id });

            foreach (var task in await db.EmployeeOnboardingTasks.ToListAsync())
            {
                await controller.ToggleTask(task.Id);
            }

            Assert.NotNull((await db.EmployeeOnboardings.FirstAsync()).CompletedAt);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task ToggleTask_ReopensOnboarding_WhenATaskIsReopened()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            var (template, employee) = await SeedTemplateAsync(db, "Site Engineer", ("Collect ID proof", 1), ("Safety induction", 3));

            var controller = new OnboardingController(db).SetUser(TestData.Principal(user, "Admin"));
            await controller.Start(new OnboardingStartFormViewModel { EmployeeId = employee.Id, OnboardingTemplateId = template.Id });

            var tasks = await db.EmployeeOnboardingTasks.ToListAsync();
            foreach (var task in tasks)
            {
                await controller.ToggleTask(task.Id);
            }

            await controller.ToggleTask(tasks[0].Id);

            Assert.Null((await db.EmployeeOnboardings.FirstAsync()).CompletedAt);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task ToggleTask_MissingTask_ReturnsNotFound()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var controller = new OnboardingController(db).SetUser(TestData.Principal(user, "Admin"));

            Assert.IsType<NotFoundResult>(await controller.ToggleTask(999));
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Index_ReportsActiveCompletedAndOverdue()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            var (template, employee) = await SeedTemplateAsync(db, "Site Engineer", ("Overdue task", 1), ("Future task", 30));

            var controller = new OnboardingController(db).SetUser(TestData.Principal(user, "Manager"));
            await controller.Start(new OnboardingStartFormViewModel { EmployeeId = employee.Id, OnboardingTemplateId = template.Id });

            // Push the first task's due date into the past.
            var overdueTask = (await db.EmployeeOnboardingTasks.OrderBy(t => t.DueDate).FirstAsync());
            overdueTask.DueDate = DateTime.UtcNow.Date.AddDays(-5);
            await db.SaveChangesAsync();

            var model = Assert.IsAssignableFrom<OnboardingListViewModel>(
                Assert.IsType<ViewResult>(await controller.Index()).Model);

            Assert.Equal(1, model.ActiveCount);
            Assert.Equal(0, model.CompletedCount);
            Assert.Equal(1, model.OverdueTaskCount);
            Assert.Single(model.OpenTasks);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Index_CompletedOnboardingIsNotCountedAsActive()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            var (template, employee) = await SeedTemplateAsync(db, "Site Engineer", ("Only task", 1));

            var controller = new OnboardingController(db).SetUser(TestData.Principal(user, "Admin"));
            await controller.Start(new OnboardingStartFormViewModel { EmployeeId = employee.Id, OnboardingTemplateId = template.Id });
            await controller.ToggleTask((await db.EmployeeOnboardingTasks.FirstAsync()).Id);

            var model = Assert.IsAssignableFrom<OnboardingListViewModel>(
                Assert.IsType<ViewResult>(await controller.Index()).Model);

            Assert.Equal(0, model.ActiveCount);
            Assert.Equal(1, model.CompletedCount);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task CreateTemplate_PersistsTasksInOrder()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var controller = new OnboardingController(db).SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.CreateTemplate(new OnboardingTemplateFormViewModel
            {
                Name = "  Warehouse Associate  ",
                Description = "Picking and packing",
                IsActive = true,
                Tasks =
                {
                    new OnboardingTaskTemplateFormViewModel { Title = "Collect ID proof", DueDayOffset = 1 },
                    new OnboardingTaskTemplateFormViewModel { Title = "  ", DueDayOffset = 2 },
                    new OnboardingTaskTemplateFormViewModel { Title = "Forklift training", DueDayOffset = 10 }
                }
            });

            Assert.IsType<RedirectToActionResult>(result);

            var template = await db.OnboardingTemplates.Include(t => t.TaskTemplates).FirstAsync();
            Assert.Equal("Warehouse Associate", template.Name);

            var tasks = template.TaskTemplates.OrderBy(t => t.DisplayOrder).ToList();
            Assert.Equal(2, tasks.Count);
            Assert.Equal("Collect ID proof", tasks[0].Title);
            Assert.Equal("Forklift training", tasks[1].Title);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task CreateTemplate_RejectsTemplateWithNoTasks()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var controller = new OnboardingController(db).SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.CreateTemplate(new OnboardingTemplateFormViewModel { Name = "Empty" });

            Assert.IsType<ViewResult>(result);
            Assert.Empty(db.OnboardingTemplates);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task CreateTemplate_RejectsDuplicateName()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            var (_, _) = await SeedTemplateAsync(db, "Site Engineer", ("Collect ID proof", 1));

            var controller = new OnboardingController(db).SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.CreateTemplate(new OnboardingTemplateFormViewModel
            {
                Name = "Site Engineer",
                Tasks = { new OnboardingTaskTemplateFormViewModel { Title = "Other task", DueDayOffset = 1 } }
            });

            Assert.IsType<ViewResult>(result);
            Assert.Single(db.OnboardingTemplates);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Details_MissingOnboarding_ReturnsNotFound()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var controller = new OnboardingController(db).SetUser(TestData.Principal(user, "Admin"));

            Assert.IsType<NotFoundResult>(await controller.Details(999));
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Start_ExcludesInactiveTemplatesFromDropdown()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();

            db.OnboardingTemplates.AddRange(
                new OnboardingTemplate { Name = "Active Template", IsActive = true },
                new OnboardingTemplate { Name = "Retired Template", IsActive = false });
            await db.SaveChangesAsync();

            var controller = new OnboardingController(db).SetUser(TestData.Principal(user, "Admin"));

            var model = Assert.IsAssignableFrom<OnboardingStartFormViewModel>(
                Assert.IsType<ViewResult>(await controller.Start()).Model);

            Assert.Equal("Active Template", Assert.Single(model.Templates).Name);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }
}