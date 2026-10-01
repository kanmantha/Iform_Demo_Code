using IForm.Web.Controllers;
using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.Services;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IForm.Tests;

/// <summary>
/// Asserts that the HR controllers hand Npgsql only Utc-kind timestamps. These run on SQLite,
/// which tolerates any kind, so they check the <see cref="DateTimeKind"/> explicitly. Without
/// this the deployed app returned 500 on every create even though the whole suite was green.
/// </summary>
public class HrTimestampKindTests
{
    private static (ApplicationDbContext Db, IDisposable Connection) NewContext() =>
        TestData.CreateDbContext();

    private static async Task<Employee> AnyEmployeeAsync(ApplicationDbContext context)
    {
        context.Employees.Add(TestData.Employee("EMP-001", "Meera", "Krishnan"));
        await context.SaveChangesAsync();
        return await context.Employees.FirstAsync();
    }

    [Fact]
    public async Task EmployeeCreate_StoresUtcKindDates()
    {
        var (context, connection) = NewContext();

        var controller = new EmployeesController(context);
        controller.SetUser(TestData.Principal(TestData.User(), "Admin"));

        var model = new EmployeeFormViewModel
        {
            EmployeeCode = "EMP-100",
            FirstName = "Asha",
            LastName = "Rao",
            Email = "asha.rao@iform.app",
            DateJoined = new DateTime(2026, 4, 15),
            DateOfBirth = new DateTime(1990, 6, 2)
        };

        var result = await controller.Create(model);
        Assert.IsType<RedirectToActionResult>(result);

        var saved = await context.Employees.SingleAsync(e => e.EmployeeCode == "EMP-100");

        Assert.Equal(DateTimeKind.Utc, saved.DateJoined!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, saved.DateOfBirth!.Value.Kind);
        Assert.Equal(new DateTime(2026, 4, 15), saved.DateJoined);
        Assert.Equal(new DateTime(1990, 6, 2), saved.DateOfBirth);
    }

    [Fact]
    public async Task EmployeeEdit_StoresUtcKindDates()
    {
        var (context, connection) = NewContext();

        var employee = await AnyEmployeeAsync(context);
        var controller = new EmployeesController(context);
        controller.SetUser(TestData.Principal(TestData.User(), "Admin"));

        var model = new EmployeeFormViewModel
        {
            Id = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            DateJoined = new DateTime(2025, 1, 20),
            DateOfBirth = new DateTime(1988, 3, 9)
        };

        var result = await controller.Edit(model);
        Assert.IsType<RedirectToActionResult>(result);

        var saved = await context.Employees.SingleAsync(e => e.Id == employee.Id);

        Assert.Equal(DateTimeKind.Utc, saved.DateJoined!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, saved.DateOfBirth!.Value.Kind);
        Assert.Equal(new DateTime(2025, 1, 20), saved.DateJoined);
    }

    [Fact]
    public async Task LeaveCreate_StoresUtcKindDates()
    {
        var (context, connection) = NewContext();

        var employee = await AnyEmployeeAsync(context);
        context.LeaveLedgerEntries.Add(new LeaveLedgerEntry
        {
            EmployeeId = employee.Id,
            LeaveType = LeaveType.Casual,
            Days = 12m,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = new LeaveController(context);
        controller.SetUser(TestData.Principal(TestData.User(), "Admin"));

        var result = await controller.Create(new LeaveRequestFormViewModel
        {
            EmployeeId = employee.Id,
            LeaveType = LeaveType.Casual,
            StartDate = new DateTime(2026, 11, 2),
            EndDate = new DateTime(2026, 11, 3),
            Reason = "Kind check"
        });

        Assert.IsType<RedirectToActionResult>(result);

        var saved = await context.LeaveRequests.SingleAsync();

        Assert.Equal(DateTimeKind.Utc, saved.StartDate.Kind);
        Assert.Equal(DateTimeKind.Utc, saved.EndDate.Kind);
        Assert.Equal(DateTimeKind.Utc, saved.CreatedAt.Kind);
        Assert.Equal(new DateTime(2026, 11, 2), saved.StartDate);
        Assert.Equal(new DateTime(2026, 11, 3), saved.EndDate);
    }

    [Fact]
    public async Task ExpenseCreate_StoresUtcKindDate()
    {
        var (context, connection) = NewContext();

        var employee = await AnyEmployeeAsync(context);
        var controller = new ExpensesController(context);
        controller.SetUser(TestData.Principal(TestData.User(), "Admin"));

        var result = await controller.Create(new ExpenseClaimFormViewModel
        {
            EmployeeId = employee.Id,
            Category = "Travel",
            Amount = 500m,
            Currency = "INR",
            ExpenseDate = new DateTime(2026, 9, 9)
        });

        Assert.IsType<RedirectToActionResult>(result);

        var saved = await context.ExpenseClaims.SingleAsync();

        Assert.Equal(DateTimeKind.Utc, saved.ExpenseDate.Kind);
        Assert.Equal(DateTimeKind.Utc, saved.CreatedAt.Kind);
        Assert.Equal(new DateTime(2026, 9, 9), saved.ExpenseDate);
    }

    [Fact]
    public async Task ExpenseApprove_StoresUtcKindDecisionTimestamp()
    {
        var (context, connection) = NewContext();

        var employee = await AnyEmployeeAsync(context);
        var claim = new ExpenseClaim
        {
            ClaimNumber = "EX-00001",
            EmployeeId = employee.Id,
            Category = "Meals",
            Amount = 100m,
            Currency = "INR",
            ExpenseDate = UtcDates.Date(new DateTime(2026, 9, 9)),
            Status = ExpenseStatus.Submitted,
            CreatedAt = DateTime.UtcNow
        };
        context.ExpenseClaims.Add(claim);
        await context.SaveChangesAsync();

        var controller = new ExpensesController(context);
        controller.SetUser(TestData.Principal(TestData.User(), "Admin"));

        await controller.Approve(claim.Id);

        var saved = await context.ExpenseClaims.SingleAsync();

        Assert.Equal(ExpenseStatus.Approved, saved.Status);
        Assert.NotNull(saved.DecidedAt);
        Assert.Equal(DateTimeKind.Utc, saved.DecidedAt!.Value.Kind);
    }

    [Fact]
    public async Task AttendanceCreate_StoresUtcKindTimes()
    {
        var (context, connection) = NewContext();

        var employee = await AnyEmployeeAsync(context);
        var controller = new AttendanceController(context);
        controller.SetUser(TestData.Principal(TestData.User(), "Admin"));

        var result = await controller.Create(new AttendanceFormViewModel
        {
            EmployeeId = employee.Id,
            Date = new DateTime(2026, 10, 1),
            CheckIn = new DateTime(2026, 10, 1, 9, 0, 0),
            CheckOut = new DateTime(2026, 10, 1, 18, 30, 0)
        });

        Assert.IsType<RedirectToActionResult>(result);

        var saved = await context.AttendanceRecords.SingleAsync();

        Assert.Equal(DateTimeKind.Utc, saved.Date.Kind);
        Assert.Equal(DateTimeKind.Utc, saved.CheckIn!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, saved.CheckOut!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, saved.CreatedAt.Kind);
        Assert.Equal(new DateTime(2026, 10, 1), saved.Date);
        Assert.Equal(9.5m, saved.HoursWorked);
    }

    [Fact]
    public async Task OnboardingStart_StoresUtcKindDates()
    {
        var (context, connection) = NewContext();

        var employee = await AnyEmployeeAsync(context);
        var template = new OnboardingTemplate { Name = "Site Engineer" };
        template.TaskTemplates.Add(new OnboardingTaskTemplate { Title = "Collect proofs", DueDayOffset = 1, DisplayOrder = 1 });
        template.TaskTemplates.Add(new OnboardingTaskTemplate { Title = "Safety induction", DueDayOffset = 3, DisplayOrder = 2 });
        context.OnboardingTemplates.Add(template);
        await context.SaveChangesAsync();

        var controller = new OnboardingController(context);
        controller.SetUser(TestData.Principal(TestData.User(), "Admin"));

        var result = await controller.Start(new OnboardingStartFormViewModel
        {
            EmployeeId = employee.Id,
            OnboardingTemplateId = template.Id
        });

        Assert.IsType<RedirectToActionResult>(result);
        var onboarding = await context.EmployeeOnboardings
            .Include(o => o.Tasks)
            .SingleAsync();

        Assert.Equal(DateTimeKind.Utc, onboarding.StartedAt.Kind);
        Assert.All(onboarding.Tasks, t => Assert.Equal(DateTimeKind.Utc, t.DueDate.Kind));
        Assert.Equal(2, onboarding.Tasks.Count);

        // Task due dates must stay two days apart in calendar terms.
        var ordered = onboarding.Tasks.OrderBy(t => t.DisplayOrder).Select(t => t.DueDate).ToList();
        Assert.Equal(2, (int)(ordered[1] - ordered[0]).TotalDays);
    }

    [Fact]
    public async Task OnboardingToggleTask_StoresUtcKindCompletionTimestamp()
    {
        var (context, connection) = NewContext();

        var employee = await AnyEmployeeAsync(context);
        var template = new OnboardingTemplate { Name = "Office" };
        context.OnboardingTemplates.Add(template);
        await context.SaveChangesAsync();

        var onboarding = new EmployeeOnboarding
        {
            EmployeeId = employee.Id,
            OnboardingTemplateId = template.Id,
            StartedAt = DateTime.UtcNow
        };
        onboarding.Tasks.Add(new EmployeeOnboardingTask
        {
            Title = "Grant access",
            DueDate = UtcDates.Date(DateTime.UtcNow),
            DisplayOrder = 1
        });
        context.EmployeeOnboardings.Add(onboarding);
        await context.SaveChangesAsync();

        var taskId = onboarding.Tasks.Single().Id;
        var controller = new OnboardingController(context);
        controller.SetUser(TestData.Principal(TestData.User(), "Admin"));

        var result = await controller.ToggleTask(taskId);
        Assert.IsType<RedirectToActionResult>(result);

        var saved = await context.EmployeeOnboardingTasks.SingleAsync();

        Assert.True(saved.IsCompleted);
        Assert.NotNull(saved.CompletedAt);
        Assert.Equal(DateTimeKind.Utc, saved.CompletedAt!.Value.Kind);
    }
}
