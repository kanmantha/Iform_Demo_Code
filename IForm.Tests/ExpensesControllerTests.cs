using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Tests;

public class ExpensesControllerTests
{
    [Fact]
    public async Task Index_ReturnsClaims_WithStatusCounts()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            db.ExpenseClaims.AddRange(
                TestData.Claim(employeeId, 500m, ExpenseStatus.Submitted, "EX-00001"),
                TestData.Claim(employeeId, 700m, ExpenseStatus.Approved, "EX-00002"),
                TestData.Claim(employeeId, 900m, ExpenseStatus.Rejected, "EX-00003"),
                TestData.Claim(employeeId, 1100m, ExpenseStatus.Reimbursed, "EX-00004"));
            await db.SaveChangesAsync();

            var controller = new ExpensesController(db).SetUser(TestData.Principal(user, "Manager"));

            var model = Assert.IsAssignableFrom<ExpenseListViewModel>(
                Assert.IsType<ViewResult>(await controller.Index(null, null)).Model);

            Assert.Equal(4, model.TotalCount);
            Assert.Equal(1, model.PendingCount);
            Assert.Equal(1, model.ApprovedCount);
            Assert.Equal(1, model.RejectedCount);
            Assert.Equal(1100m, model.ReimbursedTotal);
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

            db.ExpenseClaims.AddRange(
                TestData.Claim(employeeId, 500m, ExpenseStatus.Submitted, "EX-00001"),
                TestData.Claim(employeeId, 700m, ExpenseStatus.Approved, "EX-00002"));
            await db.SaveChangesAsync();

            var controller = new ExpensesController(db).SetUser(TestData.Principal(user, "Admin"));

            var model = Assert.IsAssignableFrom<ExpenseListViewModel>(
                Assert.IsType<ViewResult>(await controller.Index(ExpenseStatus.Approved, null)).Model);

            Assert.Equal("EX-00002", Assert.Single(model.Claims).ClaimNumber);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_PersistsClaimAsSubmitted_AndNormalisesCurrency()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            var controller = new ExpensesController(db).SetUser(TestData.Principal(user, "Manager"));

            var result = await controller.Create(new ExpenseClaimFormViewModel
            {
                EmployeeId = employeeId,
                Category = "  Travel  ",
                Description = "Taxi to site",
                Amount = 1250.75m,
                Currency = "inr",
                ExpenseDate = new DateTime(2026, 3, 2)
            });

            Assert.IsType<RedirectToActionResult>(result);

            var saved = Assert.Single(db.ExpenseClaims);
            Assert.Equal("EX-00001", saved.ClaimNumber);
            Assert.Equal("Travel", saved.Category);
            Assert.Equal("INR", saved.Currency);
            Assert.Equal(1250.75m, saved.Amount);
            Assert.Equal(ExpenseStatus.Submitted, saved.Status);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_GeneratesNextSequentialClaimNumber()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            db.ExpenseClaims.Add(TestData.Claim(employeeId, 500m, claimNumber: "EX-00001"));
            await db.SaveChangesAsync();

            var controller = new ExpensesController(db).SetUser(TestData.Principal(user, "Admin"));

            await controller.Create(new ExpenseClaimFormViewModel
            {
                EmployeeId = employeeId,
                Category = "Meals",
                Amount = 100m,
                Currency = "INR"
            });

            Assert.Equal("EX-00002", (await db.ExpenseClaims.OrderByDescending(c => c.Id).FirstAsync()).ClaimNumber);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Approve_MovesSubmittedToApproved()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            db.ExpenseClaims.Add(TestData.Claim(employeeId, 500m));
            await db.SaveChangesAsync();

            var claim = await db.ExpenseClaims.FirstAsync();
            var controller = new ExpensesController(db).SetUser(TestData.Principal(user, "Admin"));

            await controller.Approve(claim.Id);

            var saved = await db.ExpenseClaims.FirstAsync();
            Assert.Equal(ExpenseStatus.Approved, saved.Status);
            Assert.Equal(user.Id, saved.DecidedById);
            Assert.NotNull(saved.DecidedAt);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Approve_BlockedWhenClaimAlreadyDecided()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            db.ExpenseClaims.Add(TestData.Claim(employeeId, 500m, ExpenseStatus.Reimbursed));
            await db.SaveChangesAsync();

            var claim = await db.ExpenseClaims.FirstAsync();
            var controller = new ExpensesController(db).SetUser(TestData.Principal(user, "Admin"));

            await controller.Approve(claim.Id);

            Assert.Equal(ExpenseStatus.Reimbursed, (await db.ExpenseClaims.FirstAsync()).Status);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Reject_StoresDecisionNote()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            db.ExpenseClaims.Add(TestData.Claim(employeeId, 500m));
            await db.SaveChangesAsync();

            var claim = await db.ExpenseClaims.FirstAsync();
            var controller = new ExpensesController(db).SetUser(TestData.Principal(user, "Manager"));

            await controller.Reject(claim.Id, "  Missing receipt  ");

            var saved = await db.ExpenseClaims.FirstAsync();
            Assert.Equal(ExpenseStatus.Rejected, saved.Status);
            Assert.Equal("Missing receipt", saved.DecisionNote);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task MarkReimbursed_OnlyWorksFromApproved()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Employees.Add(TestData.Employee("EMP-001", "Asha", "Rao"));
            await db.SaveChangesAsync();
            var employeeId = (await db.Employees.FirstAsync()).Id;

            db.ExpenseClaims.AddRange(
                TestData.Claim(employeeId, 500m, ExpenseStatus.Approved, "EX-00001"),
                TestData.Claim(employeeId, 700m, ExpenseStatus.Submitted, "EX-00002"));
            await db.SaveChangesAsync();

            var approved = await db.ExpenseClaims.FirstAsync(c => c.ClaimNumber == "EX-00001");
            var submitted = await db.ExpenseClaims.FirstAsync(c => c.ClaimNumber == "EX-00002");

            var controller = new ExpensesController(db).SetUser(TestData.Principal(user, "Admin"));

            await controller.MarkReimbursed(approved.Id);
            await controller.MarkReimbursed(submitted.Id);

            Assert.Equal(ExpenseStatus.Reimbursed, (await db.ExpenseClaims.FirstAsync(c => c.Id == approved.Id)).Status);
            Assert.Equal(ExpenseStatus.Submitted, (await db.ExpenseClaims.FirstAsync(c => c.Id == submitted.Id)).Status);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Details_MissingClaim_ReturnsNotFound()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var controller = new ExpensesController(db).SetUser(TestData.Principal(user, "Admin"));

            Assert.IsType<NotFoundResult>(await controller.Details(999));
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Approve_MissingClaim_ReturnsNotFound()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var controller = new ExpensesController(db).SetUser(TestData.Principal(user, "Admin"));

            Assert.IsType<NotFoundResult>(await controller.Approve(999));
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }
}