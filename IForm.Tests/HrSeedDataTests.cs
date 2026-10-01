using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IForm.Tests;

/// <summary>
/// Checks the HR demo data that DbSeeder writes is internally consistent, so a
/// freshly provisioned database looks like real usage rather than broken rows.
/// </summary>
public class HrSeedDataTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HrSeedDataTests(WebApplicationFactory<Program> factory)
    {
        var tempDb = Path.Combine(Path.GetTempPath(), $"iform-hrseed-{Guid.NewGuid():N}.db");
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={tempDb}");
        });
    }

    private IServiceScope NewScope() => _factory.Services.CreateScope();

    [Fact]
    public void Seed_ProvidesDepartmentsEmployeesAndReportingLines()
    {
        using var scope = NewScope();
        var db = scope.ServiceProvider.GetRequiredService<IForm.Web.Data.ApplicationDbContext>();

        Assert.True(db.Departments.Count() >= 8);
        Assert.True(db.Employees.Count() >= 15);

        // Every employee belongs to a real department.
        Assert.DoesNotContain(db.Employees, e => e.DepartmentId == 0);

        // Managers are themselves employees, and no one reports to themselves.
        Assert.DoesNotContain(db.Employees, e => e.ManagerId.HasValue && e.ManagerId == e.Id);

        var employeeIds = db.Employees.Select(e => e.Id).ToList();
        Assert.All(
            db.Employees.Where(e => e.ManagerId.HasValue).Select(e => e.ManagerId!.Value),
            managerId => Assert.Contains(managerId, employeeIds));

        // At least one root of the reporting tree, and no manager chain loops.
        Assert.True(db.Employees.Any(e => e.ManagerId == null));
        Assert.True(db.Employees.Count(e => e.ManagerId != null) >= 10);

        var managerOf = db.Employees
            .Where(e => e.ManagerId.HasValue)
            .ToDictionary(e => e.Id, e => e.ManagerId!.Value);

        foreach (var employeeId in managerOf.Keys)
        {
            var seen = new HashSet<int>();
            var current = employeeId;

            while (managerOf.TryGetValue(current, out var managerId))
            {
                Assert.True(seen.Add(current), $"Reporting cycle detected at employee {current}.");
                current = managerId;
            }
        }
    }

    [Fact]
    public void Seed_LeaveRequests_MatchWorkingDayCountAndLedger()
    {
        using var scope = NewScope();
        var db = scope.ServiceProvider.GetRequiredService<IForm.Web.Data.ApplicationDbContext>();

        var requests = db.LeaveRequests.Include(r => r.Employee).ToList();
        Assert.True(requests.Count >= 10);

        var holidays = new HashSet<DateTime>();

        foreach (var request in requests)
        {
            var calculated = LeaveCalculator.CountWorkingDays(request.StartDate, request.EndDate, holidays);

            Assert.Equal(calculated, request.Days);
            Assert.True(request.StartDate <= request.EndDate);
            Assert.False(request.Reason == null || request.Reason.Trim().Length == 0);
            Assert.True(request.StartDate.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday),
                $"{request.RequestNumber} starts on a weekend.");

            if (request.Status == LeaveStatus.Pending)
            {
                Assert.Null(request.DecidedAt);
            }
            else
            {
                Assert.NotNull(request.DecidedAt);
            }
        }

        // Every approved request has exactly one matching ledger deduction.
        foreach (var approved in requests.Where(r => r.Status == LeaveStatus.Approved))
        {
            var entry = db.LeaveLedgerEntries.SingleOrDefault(l => l.LeaveRequestId == approved.Id);

            Assert.NotNull(entry);
            Assert.Equal(-approved.Days, entry!.Days);
        }

        // Rejected requests must not have touched the ledger.
        Assert.DoesNotContain(
            db.LeaveLedgerEntries,
            l => requests.Any(r => r.Id == l.LeaveRequestId && r.Status == LeaveStatus.Rejected));

        // No employee ever has a negative balance.
        foreach (var group in db.LeaveLedgerEntries.GroupBy(l => new { l.EmployeeId, l.LeaveType }))
        {
            Assert.True(group.Sum(l => l.Days) >= 0,
                $"{group.Key.EmployeeId}/{group.Key.LeaveType} has a negative balance.");
        }
    }

    [Fact]
    public void Seed_LeaveLedger_GrantsAtLeastTheConsumedAmount()
    {
        using var scope = NewScope();
        var db = scope.ServiceProvider.GetRequiredService<IForm.Web.Data.ApplicationDbContext>();

        var grants = db.LeaveLedgerEntries
            .Where(l => l.Days > 0)
            .GroupBy(l => new { l.EmployeeId, l.LeaveType })
            .Select(g => new
            {
                g.Key.EmployeeId,
                g.Key.LeaveType,
                Granted = g.Sum(l => l.Days)
            })
            .ToList();

        Assert.True(grants.Count >= 8);

        foreach (var grant in grants)
        {
            var used = db.LeaveLedgerEntries
                .Where(l => l.EmployeeId == grant.EmployeeId && l.LeaveType == grant.LeaveType && l.Days < 0)
                .Sum(l => (decimal?)l.Days) ?? 0m;

            Assert.True(-used <= grant.Granted);
        }
    }

    [Fact]
    public void Seed_ExpenseClaims_HaveConsistentWorkflowState()
    {
        using var scope = NewScope();
        var db = scope.ServiceProvider.GetRequiredService<IForm.Web.Data.ApplicationDbContext>();

        var claims = db.ExpenseClaims.ToList();
        Assert.True(claims.Count >= 8);

        foreach (var claim in claims)
        {
            Assert.True(claim.Amount > 0);
            Assert.Equal("INR", claim.Currency);
            Assert.False(string.IsNullOrWhiteSpace(claim.Description));

            if (claim.Status == ExpenseStatus.Rejected)
            {
                Assert.NotNull(claim.DecisionNote);
            }

            if (claim.Status == ExpenseStatus.Submitted)
            {
                Assert.Null(claim.DecidedAt);
            }
            else
            {
                Assert.NotNull(claim.DecidedAt);
                Assert.NotNull(claim.DecidedById);
            }
        }

        Assert.Contains(claims, c => c.Status == ExpenseStatus.Approved);
        Assert.Contains(claims, c => c.Status == ExpenseStatus.Submitted);
        Assert.Contains(claims, c => c.Status == ExpenseStatus.Reimbursed);
    }

    [Fact]
    public void Seed_AttendanceRecords_AgreeWithCalculatedStatus()
    {
        using var scope = NewScope();
        var db = scope.ServiceProvider.GetRequiredService<IForm.Web.Data.ApplicationDbContext>();

        var records = db.AttendanceRecords.ToList();
        Assert.True(records.Count >= 10);

        foreach (var record in records)
        {
            Assert.InRange(record.HoursWorked, 0m, 16m);
            Assert.Equal(AttendanceCalculator.StatusFor(record.HoursWorked), record.Status);

            if (record.HoursWorked == 0)
            {
                Assert.Null(record.CheckIn);
                Assert.Null(record.CheckOut);
            }
        }

        // One record per employee per day.
        Assert.Equal(
            records.Count,
            records.Select(r => new { r.EmployeeId, r.Date }).Distinct().Count());
    }

    [Fact]
    public void Seed_OnboardingTemplates_AreActiveWithOrderedTasks()
    {
        using var scope = NewScope();
        var db = scope.ServiceProvider.GetRequiredService<IForm.Web.Data.ApplicationDbContext>();

        var templates = db.OnboardingTemplates.Include(t => t.TaskTemplates).ToList();
        Assert.True(templates.Count >= 2);

        foreach (var template in templates)
        {
            Assert.True(template.IsActive);
            Assert.True(template.TaskTemplates.Count >= 4);
            Assert.All(template.TaskTemplates, t => Assert.False(string.IsNullOrWhiteSpace(t.Title)));
            Assert.All(template.TaskTemplates, t => Assert.InRange(t.DueDayOffset, 1, 365));
        }
    }

    [Fact]
    public void Seed_EmployeeOnboardings_HaveDueDatesMatchingOffsets()
    {
        using var scope = NewScope();
        var db = scope.ServiceProvider.GetRequiredService<IForm.Web.Data.ApplicationDbContext>();

        var onboardings = db.EmployeeOnboardings.Include(o => o.Tasks).ToList();
        Assert.True(onboardings.Count >= 2);

        foreach (var onboarding in onboardings)
        {
            Assert.True(onboarding.Tasks.Count > 0);

            // Completed tasks must carry a completion timestamp, and open ones must not.
            foreach (var task in onboarding.Tasks)
            {
                if (task.IsCompleted)
                {
                    Assert.NotNull(task.CompletedAt);
                }
                else
                {
                    Assert.Null(task.CompletedAt);
                }
            }
        }

        // At least one overdue task exists so the dashboard badge is exercised.
        Assert.Contains(onboardings.SelectMany(o => o.Tasks), t => !t.IsCompleted && t.DueDate < DateTime.UtcNow);
    }
}