using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Tests;

public class LeaveControllerTests
{
    private static readonly DateTime Monday = new(2026, 3, 2);
    private static readonly DateTime Friday = new(2026, 3, 6);

    [Fact]
    public async Task Index_ReturnsRequests_WithStatusCounts()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            db.LeaveRequests.AddRange(
                TestData.Leave(employeeId, LeaveType.Casual, Monday, Friday, 5m, requestNumber: "LV-00001"),
                TestData.Leave(employeeId, LeaveType.Sick, Monday, Friday, 5m, LeaveStatus.Approved, "LV-00002"),
                TestData.Leave(employeeId, LeaveType.Earned, Monday, Friday, 5m, LeaveStatus.Rejected, "LV-00003"));
            await db.SaveChangesAsync();

            var controller = new LeaveController(db).SetUser(TestData.Principal(user, "Manager"));

            var model = Assert.IsAssignableFrom<LeaveListViewModel>(
                Assert.IsType<ViewResult>(await controller.Index(null, null, null)).Model);

            Assert.Equal(3, model.TotalCount);
            Assert.Equal(1, model.PendingCount);
            Assert.Equal(1, model.ApprovedCount);
            Assert.Equal(1, model.RejectedCount);
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

            db.LeaveRequests.AddRange(
                TestData.Leave(employeeId, LeaveType.Casual, Monday, Friday, 5m, requestNumber: "LV-00001"),
                TestData.Leave(employeeId, LeaveType.Sick, Monday, Friday, 5m, LeaveStatus.Approved, "LV-00002"));
            await db.SaveChangesAsync();

            var controller = new LeaveController(db).SetUser(TestData.Principal(user, "Admin"));

            var model = Assert.IsAssignableFrom<LeaveListViewModel>(
                Assert.IsType<ViewResult>(await controller.Index(LeaveStatus.Approved, null, null)).Model);

            Assert.Equal("LV-00002", Assert.Single(model.LeaveRequests).RequestNumber);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_CountsWorkingDays_AndPersistsAsPending()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            var controller = new LeaveController(db).SetUser(TestData.Principal(user, "Manager"));

            var result = await controller.Create(new LeaveRequestFormViewModel
            {
                EmployeeId = employeeId,
                LeaveType = LeaveType.Casual,
                StartDate = Monday,
                EndDate = Friday,
                Reason = "Family function"
            });

            Assert.IsType<RedirectToActionResult>(result);

            var saved = Assert.Single(db.LeaveRequests);
            Assert.Equal(5m, saved.Days);
            Assert.Equal(LeaveStatus.Pending, saved.Status);
            Assert.Equal("LV-00001", saved.RequestNumber);
            Assert.Null(saved.DecidedAt);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_RejectsEndDateBeforeStartDate()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            var controller = new LeaveController(db).SetUser(TestData.Principal(user, "Manager"));

            var result = await controller.Create(new LeaveRequestFormViewModel
            {
                EmployeeId = employeeId,
                LeaveType = LeaveType.Casual,
                StartDate = Friday,
                EndDate = Monday,
                Reason = "Reversed range"
            });

            Assert.IsType<ViewResult>(result);
            Assert.Empty(db.LeaveRequests);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_RejectsRangeWithNoWorkingDays()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            var controller = new LeaveController(db).SetUser(TestData.Principal(user, "Manager"));

            var result = await controller.Create(new LeaveRequestFormViewModel
            {
                EmployeeId = employeeId,
                LeaveType = LeaveType.Casual,
                StartDate = new DateTime(2026, 3, 7),
                EndDate = new DateTime(2026, 3, 8),
                Reason = "Weekend only"
            });

            Assert.IsType<ViewResult>(result);
            Assert.Empty(db.LeaveRequests);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_RejectsOverlappingPendingRequest()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            db.LeaveRequests.Add(TestData.Leave(employeeId, LeaveType.Casual, Monday, Friday, 5m));
            await db.SaveChangesAsync();

            var controller = new LeaveController(db).SetUser(TestData.Principal(user, "Manager"));

            var result = await controller.Create(new LeaveRequestFormViewModel
            {
                EmployeeId = employeeId,
                LeaveType = LeaveType.Casual,
                StartDate = Monday.AddDays(2),
                EndDate = Monday.AddDays(4),
                Reason = "Overlapping"
            });

            Assert.IsType<ViewResult>(result);
            Assert.Single(db.LeaveRequests);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Approve_DeductsFromLedger_AndSetsApproved()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            db.LeaveLedgerEntries.Add(TestData.Grant(employeeId, LeaveType.Casual, 12m));
            db.LeaveRequests.Add(TestData.Leave(employeeId, LeaveType.Casual, Monday, Friday, 5m));
            await db.SaveChangesAsync();

            var request = await db.LeaveRequests.FirstAsync();
            var controller = new LeaveController(db).SetUser(TestData.Principal(user, "Admin"));

            var result = await controller.Approve(request.Id);

            Assert.IsType<RedirectToActionResult>(result);

            var saved = await db.LeaveRequests.FirstAsync();
            Assert.Equal(LeaveStatus.Approved, saved.Status);
            Assert.Equal(user.Id, saved.DecidedById);
            Assert.NotNull(saved.DecidedAt);

            var consumption = Assert.Single(db.LeaveLedgerEntries.Where(l => l.Days < 0));
            Assert.Equal(-5m, consumption.Days);
            Assert.Equal(request.Id, consumption.LeaveRequestId);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Approve_BlockedWhenBalanceIsShort()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            db.LeaveLedgerEntries.Add(TestData.Grant(employeeId, LeaveType.Casual, 2m));
            db.LeaveRequests.Add(TestData.Leave(employeeId, LeaveType.Casual, Monday, Friday, 5m));
            await db.SaveChangesAsync();

            var request = await db.LeaveRequests.FirstAsync();
            var controller = new LeaveController(db).SetUser(TestData.Principal(user, "Admin"));

            await controller.Approve(request.Id);

            Assert.Equal(LeaveStatus.Pending, (await db.LeaveRequests.FirstAsync()).Status);
            Assert.DoesNotContain(db.LeaveLedgerEntries, l => l.Days < 0);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Approve_BlockedWhenAnotherRequestAlreadyApprovedInRange()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            db.LeaveLedgerEntries.Add(TestData.Grant(employeeId, LeaveType.Casual, 20m));
            db.LeaveRequests.AddRange(
                TestData.Leave(employeeId, LeaveType.Casual, Monday, Friday, 5m, LeaveStatus.Approved, "LV-00001"),
                TestData.Leave(employeeId, LeaveType.Casual, Monday.AddDays(1), Monday.AddDays(3), 3m, requestNumber: "LV-00002"));
            await db.SaveChangesAsync();

            var request = await db.LeaveRequests.FirstAsync(r => r.RequestNumber == "LV-00002");
            var controller = new LeaveController(db).SetUser(TestData.Principal(user, "Admin"));

            await controller.Approve(request.Id);

            Assert.Equal(LeaveStatus.Pending, (await db.LeaveRequests.FirstAsync(r => r.Id == request.Id)).Status);
            Assert.DoesNotContain(db.LeaveLedgerEntries, l => l.Days < 0);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Approve_BlockedWhenRequestAlreadyDecided()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            db.LeaveLedgerEntries.Add(TestData.Grant(employeeId, LeaveType.Casual, 20m));
            db.LeaveRequests.Add(TestData.Leave(employeeId, LeaveType.Casual, Monday, Friday, 5m, LeaveStatus.Rejected));
            await db.SaveChangesAsync();

            var request = await db.LeaveRequests.FirstAsync();
            var controller = new LeaveController(db).SetUser(TestData.Principal(user, "Admin"));

            await controller.Approve(request.Id);

            Assert.Equal(LeaveStatus.Rejected, (await db.LeaveRequests.FirstAsync()).Status);
            Assert.DoesNotContain(db.LeaveLedgerEntries, l => l.Days < 0);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Approve_UsesOnlyTheMatchingLeaveTypeBalance()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            // Plenty of earned leave, but no casual entitlement.
            db.LeaveLedgerEntries.Add(TestData.Grant(employeeId, LeaveType.Earned, 30m));
            db.LeaveRequests.Add(TestData.Leave(employeeId, LeaveType.Casual, Monday, Friday, 5m));
            await db.SaveChangesAsync();

            var request = await db.LeaveRequests.FirstAsync();
            var controller = new LeaveController(db).SetUser(TestData.Principal(user, "Admin"));

            await controller.Approve(request.Id);

            Assert.Equal(LeaveStatus.Pending, (await db.LeaveRequests.FirstAsync()).Status);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Approve_AllowsAdjacentNonOverlappingRequest()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            db.LeaveLedgerEntries.Add(TestData.Grant(employeeId, LeaveType.Casual, 20m));
            db.LeaveRequests.AddRange(
                TestData.Leave(employeeId, LeaveType.Casual, Monday, Monday, 1m, LeaveStatus.Approved, "LV-00001"),
                TestData.Leave(employeeId, LeaveType.Casual, Monday.AddDays(1), Monday.AddDays(5), 5m, requestNumber: "LV-00002"));
            await db.SaveChangesAsync();

            var request = await db.LeaveRequests.FirstAsync(r => r.RequestNumber == "LV-00002");
            var controller = new LeaveController(db).SetUser(TestData.Principal(user, "Admin"));

            await controller.Approve(request.Id);

            Assert.Equal(LeaveStatus.Approved, (await db.LeaveRequests.FirstAsync(r => r.Id == request.Id)).Status);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Approve_MissingRequest_ReturnsNotFound()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var controller = new LeaveController(db).SetUser(TestData.Principal(user, "Admin"));

            Assert.IsType<NotFoundResult>(await controller.Approve(999));
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Reject_SetsRejectedWithoutTouchingLedger()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            db.LeaveLedgerEntries.Add(TestData.Grant(employeeId, LeaveType.Casual, 12m));
            db.LeaveRequests.Add(TestData.Leave(employeeId, LeaveType.Casual, Monday, Friday, 5m));
            await db.SaveChangesAsync();

            var request = await db.LeaveRequests.FirstAsync();
            var controller = new LeaveController(db).SetUser(TestData.Principal(user, "Manager"));

            await controller.Reject(request.Id, "  Peak season, cannot spare you  ");

            var saved = await db.LeaveRequests.FirstAsync();
            Assert.Equal(LeaveStatus.Rejected, saved.Status);
            Assert.Equal("Peak season, cannot spare you", saved.DecisionNote);
            Assert.DoesNotContain(db.LeaveLedgerEntries, l => l.Days < 0);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Reject_WithoutNote_UsesDefaultExplanation()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            db.LeaveRequests.Add(TestData.Leave(employeeId, LeaveType.Casual, Monday, Friday, 5m));
            await db.SaveChangesAsync();

            var request = await db.LeaveRequests.FirstAsync();
            var controller = new LeaveController(db).SetUser(TestData.Principal(user, "Admin"));

            await controller.Reject(request.Id, "   ");

            Assert.Equal("Rejected by approver.", (await db.LeaveRequests.FirstAsync()).DecisionNote);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Details_ReturnsBalanceAndRecentLedger()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            db.LeaveLedgerEntries.Add(TestData.Grant(employeeId, LeaveType.Casual, 12m));
            db.LeaveRequests.Add(TestData.Leave(employeeId, LeaveType.Casual, Monday, Friday, 5m));
            await db.SaveChangesAsync();

            var request = await db.LeaveRequests.FirstAsync();
            var controller = new LeaveController(db).SetUser(TestData.Principal(user, "Admin"));

            var model = Assert.IsAssignableFrom<LeaveDetailViewModel>(
                Assert.IsType<ViewResult>(await controller.Details(request.Id)).Model);

            Assert.Equal(12m, model.BalanceBeforeDecision);
            Assert.Single(model.RecentLedger);
            Assert.Equal("EMP-001", model.Request.Employee?.EmployeeCode);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Details_MissingRequest_ReturnsNotFound()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var controller = new LeaveController(db).SetUser(TestData.Principal(user, "Admin"));

            Assert.IsType<NotFoundResult>(await controller.Details(999));
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Approve_Twice_OnlyDeductsOnce()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            db.LeaveLedgerEntries.Add(TestData.Grant(employeeId, LeaveType.Casual, 12m));
            db.LeaveRequests.Add(TestData.Leave(employeeId, LeaveType.Casual, Monday, Friday, 5m));
            await db.SaveChangesAsync();

            var request = await db.LeaveRequests.FirstAsync();
            var controller = new LeaveController(db).SetUser(TestData.Principal(user, "Admin"));

            await controller.Approve(request.Id);
            await controller.Approve(request.Id);

            Assert.Single(db.LeaveLedgerEntries.Where(l => l.Days < 0));
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }
}