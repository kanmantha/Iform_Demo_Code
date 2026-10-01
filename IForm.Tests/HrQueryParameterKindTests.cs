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
/// Live verification showed Npgsql also rejects Unspecified-kind timestamps used as
/// <em>query parameters</em>, not just as values being written. These tests assert the
/// parameters the HR controllers build are Utc, so filtering and duplicate checks cannot
/// throw against timestamptz columns the way a blind stamp-at-save fix missed.
/// </summary>
public class HrQueryParameterKindTests
{
    private static (ApplicationDbContext Db, IDisposable Connection) NewContext() =>
        TestData.CreateDbContext();

    private static async Task<Employee> SeedEmployeeAsync(ApplicationDbContext context)
    {
        context.Employees.Add(TestData.Employee("EMP-001", "Meera", "Krishnan"));
        await context.SaveChangesAsync();
        return await context.Employees.FirstAsync();
    }

    [Fact]
    public async Task AttendanceIndex_DateRangeFilterBindsUtcParameters()
    {
        var (context, connection) = NewContext();
        var employee = await SeedEmployeeAsync(context);

        // Two records seeded through the controller so the stored values are Utc.
        context.AttendanceRecords.AddRange(
            new AttendanceRecord
            {
                EmployeeId = employee.Id,
                Date = UtcDates.Date(new DateTime(2026, 10, 1)),
                HoursWorked = 9m,
                Status = AttendanceStatus.Present,
                CreatedAt = DateTime.UtcNow
            },
            new AttendanceRecord
            {
                EmployeeId = employee.Id,
                Date = UtcDates.Date(new DateTime(2026, 10, 20)),
                HoursWorked = 8m,
                Status = AttendanceStatus.Present,
                CreatedAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var controller = new AttendanceController(context)
            .SetUser(TestData.Principal(TestData.User(), "Admin"));

        // The values below stand in for query-string input, which is Unspecified.
        var from = new DateTime(2026, 10, 5);
        var to = new DateTime(2026, 10, 15);
        Assert.Equal(DateTimeKind.Unspecified, from.Kind);

        var model = Assert.IsAssignableFrom<AttendanceListViewModel>(
            Assert.IsType<ViewResult>(await controller.Index(from, to, null, null)).Model);

        // Neither in-range date should appear.
        Assert.DoesNotContain(model.Records, r => r.Date == UtcDates.Date(new DateTime(2026, 10, 1)));
        Assert.DoesNotContain(model.Records, r => r.Date == UtcDates.Date(new DateTime(2026, 10, 20)));
        Assert.Equal(0, model.TotalCount);

        // A range that contains only the first record must find exactly that one.
        var hit = Assert.IsAssignableFrom<AttendanceListViewModel>(
            Assert.IsType<ViewResult>(await controller.Index(new DateTime(2026, 9, 1), new DateTime(2026, 10, 10), null, null)).Model);
        Assert.Equal(1, hit.TotalCount);
        Assert.Equal(UtcDates.Date(new DateTime(2026, 10, 1)), hit.Records.Single().Date);

        // The view model gets plain calendar dates back so the filter renders correctly.
        Assert.Equal(new DateTime(2026, 10, 5), model.From);
        Assert.Equal(new DateTime(2026, 10, 15), model.To);
    }

    [Fact]
    public async Task AttendanceCreate_DuplicateCheckUsesUtcBoundDate()
    {
        var (context, connection) = NewContext();
        var employee = await SeedEmployeeAsync(context);

        context.AttendanceRecords.Add(new AttendanceRecord
        {
            EmployeeId = employee.Id,
            Date = UtcDates.Date(new DateTime(2026, 10, 1)),
            HoursWorked = 9m,
            Status = AttendanceStatus.Present,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = new AttendanceController(context)
            .SetUser(TestData.Principal(TestData.User(), "Admin"));

        var result = await controller.Create(new AttendanceFormViewModel
        {
            EmployeeId = employee.Id,
            Date = new DateTime(2026, 10, 1),
            CheckIn = new DateTime(2026, 10, 1, 9, 0, 0),
            CheckOut = new DateTime(2026, 10, 1, 17, 0, 0)
        });

        // The duplicate must be caught by validation rather than hitting a unique-index violation.
        var view = Assert.IsType<ViewResult>(result);
        var errors = string.Join(" ",
            view.ViewData.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));

        Assert.Contains("already recorded", errors, StringComparison.OrdinalIgnoreCase);
        Assert.Single(await context.AttendanceRecords.ToListAsync());
    }

    [Fact]
    public async Task LeaveCreate_OverlapCheckUsesUtcBoundRange()
    {
        var (context, connection) = NewContext();
        var employee = await SeedEmployeeAsync(context);

        context.LeaveLedgerEntries.Add(new LeaveLedgerEntry
        {
            EmployeeId = employee.Id,
            LeaveType = LeaveType.Casual,
            Days = 12m,
            CreatedAt = DateTime.UtcNow
        });
        context.LeaveRequests.Add(new LeaveRequest
        {
            RequestNumber = "LV-00001",
            EmployeeId = employee.Id,
            LeaveType = LeaveType.Casual,
            StartDate = UtcDates.Date(new DateTime(2026, 12, 7)),
            EndDate = UtcDates.Date(new DateTime(2026, 12, 9)),
            Days = 3m,
            Reason = "Existing",
            Status = LeaveStatus.Approved,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Overlapping request must be rejected by validation, not by a query exception.
        var overlap = await new LeaveController(context)
            .SetUser(TestData.Principal(TestData.User(), "Admin"))
            .Create(new LeaveRequestFormViewModel
        {
            EmployeeId = employee.Id,
            LeaveType = LeaveType.Casual,
            StartDate = new DateTime(2026, 12, 8),
            EndDate = new DateTime(2026, 12, 10),
            Reason = "Overlap"
        });

        var errors = string.Join(" ",
            Assert.IsType<ViewResult>(overlap).ViewData.ModelState.Values
                .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
        Assert.Contains("already has leave", errors, StringComparison.OrdinalIgnoreCase);
        Assert.Single(await context.LeaveRequests.ToListAsync());

        // A clear range must succeed and store Utc dates. A fresh controller is used because
        // a rejected render leaves its ModelState errors on the shared ViewData.
        var created = await new LeaveController(context)
            .SetUser(TestData.Principal(TestData.User(), "Admin"))
            .Create(new LeaveRequestFormViewModel
        {
            EmployeeId = employee.Id,
            LeaveType = LeaveType.Casual,
            StartDate = new DateTime(2026, 12, 14),
            EndDate = new DateTime(2026, 12, 15),
            Reason = "Clear range"
        });

        var createdView = created as ViewResult;
        if (createdView is not null)
        {
            var messages = string.Join(" | ",
                createdView.ViewData.ModelState.Values
                    .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            Assert.Fail($"Expected a redirect but got validation errors: {messages}");
        }

        Assert.IsType<RedirectToActionResult>(created);

        var saved = await context.LeaveRequests
            .SingleAsync(r => r.Reason == "Clear range");
        Assert.Equal(DateTimeKind.Utc, saved.StartDate.Kind);
        Assert.Equal(DateTimeKind.Utc, saved.EndDate.Kind);
    }
}