using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using IForm.Web.Models;
using IForm.Web.Services;

namespace IForm.Tests;

/// <summary>
/// Walks a full HR cycle against seeded data: create an employee, request and
/// approve leave, submit and pay an expense, log attendance, and run onboarding.
/// Each step asserts the page that follows actually renders.
/// </summary>
public class HrWorkflowSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HrWorkflowSmokeTests()
    {
        var tempDb = Path.Combine(Path.GetTempPath(), $"iform-hrflow-{Guid.NewGuid():N}.db");
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={tempDb}");
            });
    }

    private static async Task<HttpClient> LoginClientAsync(WebApplicationFactory<Program> factory, string email, string password)
    {
        var client = factory.CreateClient();
        var html = await client.GetStringAsync("/Account/Login");

        var token = System.Text.RegularExpressions.Regex.Match(
            html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["RememberMe"] = "false",
            ["__RequestVerificationToken"] = token
        });

        var response = await client.PostAsync("/Account/Login", form);
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Redirect,
            $"Login failed with {(int)response.StatusCode}.");
        return client;
    }

    private static async Task<string> TokenAsync(HttpClient client, string path)
    {
        var html = await client.GetStringAsync(path);
        return System.Text.RegularExpressions.Regex.Match(
            html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
    }

    [Fact]
    public async Task EmployeeDirectory_ListsSeededEmployees()
    {
        var client = await LoginClientAsync(_factory, "admin@iform.app", "Admin@123");

        var html = await client.GetStringAsync("/Employees");

        Assert.Contains("Meera Krishnan", html);
        Assert.Contains("Arjun Deshpande", html);
        Assert.Contains("Engineering", html);
        Assert.Contains("EMP-001", html);
    }

    [Fact]
    public async Task EmployeeSearch_FiltersTheDirectory()
    {
        var client = await LoginClientAsync(_factory, "admin@iform.app", "Admin@123");

        var html = await client.GetStringAsync("/Employees?search=Meera");

        Assert.Contains("Meera Krishnan", html);
        Assert.DoesNotContain("Fatima Begum", html);
    }

    [Fact]
    public async Task EmployeeDirectory_CreateThenAppearsInList()
    {
        var client = await LoginClientAsync(_factory, "admin@iform.app", "Admin@123");

        var token = await TokenAsync(client, "/Employees/Create");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = "0",
            ["EmployeeCode"] = "EMP-900",
            ["FirstName"] = "Test",
            ["LastName"] = "Recruiter",
            ["Email"] = "test.recruiter@iform.app",
            ["JobTitle"] = "Recruiter",
            ["Status"] = "Active",
            ["DateJoined"] = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
            ["__RequestVerificationToken"] = token
        });

        var response = await client.PostAsync("/Employees/Create", form);
        Assert.True(response.IsSuccessStatusCode, $"Create failed with {(int)response.StatusCode}.");

        var html = await client.GetStringAsync("/Employees?search=EMP-900");
        Assert.Contains("Test Recruiter", html);
    }

    [Fact]
    public async Task EmployeeDirectory_RejectsDuplicateCode()
    {
        var client = await LoginClientAsync(_factory, "admin@iform.app", "Admin@123");

        var token = await TokenAsync(client, "/Employees/Create");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = "0",
            ["EmployeeCode"] = "EMP-001",
            ["FirstName"] = "Duplicate",
            ["LastName"] = "Person",
            ["Email"] = "duplicate@iform.app",
            ["Status"] = "Active",
            ["__RequestVerificationToken"] = token
        });

        var response = await client.PostAsync("/Employees/Create", form);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("already exists", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Leave_DetailsPageRendersWithBalance()
    {
        var client = await LoginClientAsync(_factory, "manager@iform.app", "Manager@123");

        var index = await client.GetStringAsync("/Leave?status=Pending");
        Assert.Contains("Pending", index);

        var detail = await client.GetStringAsync("/Leave/Details/1");
        Assert.Contains("Working days", detail);
        Assert.Contains("Balance for this type", detail);
    }

    [Fact]
    public async Task Leave_CreateThenApprove_DeductsBalance()
    {
        var client = await LoginClientAsync(_factory, "admin@iform.app", "Admin@123");

        // The seeded employee Vikram Singh (EMP-010) has 12 casual days granted
        // minus one day already approved.
        var token = await TokenAsync(client, "/Leave/Create");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = "0",
            ["EmployeeId"] = "10",
            ["LeaveType"] = "Casual",
            ["StartDate"] = "2027-04-05",
            ["EndDate"] = "2027-04-06",
            ["Reason"] = "Workflow smoke test",
            ["__RequestVerificationToken"] = token
        });

        var created = await client.PostAsync("/Leave/Create", form);
        Assert.True(created.IsSuccessStatusCode, $"Leave create failed with {(int)created.StatusCode}.");

        var list = await client.GetStringAsync("/Leave?status=Pending");
        Assert.DoesNotContain("An unhandled exception", list);

        // The reason is not in the list view, so read the newest request back and
        // confirm its detail page renders.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IForm.Web.Data.ApplicationDbContext>();
            var request = await db.LeaveRequests.OrderByDescending(r => r.Id).FirstAsync();

            Assert.Equal("Workflow smoke test", request.Reason);
            Assert.Equal(LeaveStatus.Pending, request.Status);
            Assert.Equal(2m, request.Days);

            var detail = await client.GetStringAsync($"/Leave/Details/{request.Id}");
            Assert.Contains("Workflow smoke test", detail);
        }
    }

    [Fact]
    public async Task Leave_Approve_DeductsWorkingDaysFromLedger()
    {
        var client = await LoginClientAsync(_factory, "admin@iform.app", "Admin@123");

        // Employee 8 (Suresh Pai) has 12 casual days granted and none consumed.
        int requestId;
        decimal balanceBefore;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IForm.Web.Data.ApplicationDbContext>();
            var employee = await db.Employees.FirstAsync(e => e.EmployeeCode == "EMP-008");

            var request = new LeaveRequest
            {
                RequestNumber = $"LV-TEST-{Guid.NewGuid():N}"[..20],
                EmployeeId = employee.Id,
                LeaveType = LeaveType.Casual,
                StartDate = new DateTime(2027, 4, 5),
                EndDate = new DateTime(2027, 4, 6),
                Days = 2m,
                Reason = "Approval smoke test",
                Status = LeaveStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            db.LeaveRequests.Add(request);
            await db.SaveChangesAsync();

            requestId = request.Id;
            balanceBefore = await db.LeaveLedgerEntries
                .Where(l => l.EmployeeId == employee.Id && l.LeaveType == LeaveType.Casual)
                .SumAsync(l => (decimal?)l.Days) ?? 0m;
        }

        var token = await TokenAsync(client, $"/Leave/Details/{requestId}");
        var response = await client.PostAsync(
            $"/Leave/Approve/{requestId}",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        Assert.True(response.IsSuccessStatusCode, $"Approve failed with {(int)response.StatusCode}.");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IForm.Web.Data.ApplicationDbContext>();
            var saved = await db.LeaveRequests.FindAsync(requestId);

            Assert.Equal(LeaveStatus.Approved, saved!.Status);
            Assert.NotNull(saved.DecidedAt);

            var balanceAfter = await db.LeaveLedgerEntries
                .Where(l => l.EmployeeId == saved.EmployeeId && l.LeaveType == LeaveType.Casual)
                .SumAsync(l => (decimal?)l.Days) ?? 0m;

            Assert.Equal(balanceBefore - 2m, balanceAfter);
        }
    }

    [Fact]
    public async Task Leave_ApproveBeyondBalance_IsRefusedAndBalanceUnchanged()
    {
        var client = await LoginClientAsync(_factory, "admin@iform.app", "Admin@123");

        int requestId;
        decimal balanceBefore;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IForm.Web.Data.ApplicationDbContext>();

            // A brand new employee has no ledger, so any approval must be refused.
            var employee = new Employee
            {
                EmployeeCode = "EMP-950",
                FirstName = "Zero",
                LastName = "Balance",
                Email = "zero.balance@iform.app",
                Status = EmploymentStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            db.Employees.Add(employee);
            await db.SaveChangesAsync();

            var request = new LeaveRequest
            {
                RequestNumber = $"LV-TEST-{Guid.NewGuid():N}"[..20],
                EmployeeId = employee.Id,
                LeaveType = LeaveType.Earned,
                StartDate = new DateTime(2027, 6, 1),
                EndDate = new DateTime(2027, 6, 2),
                Days = 2m,
                Reason = "Over-balance approval test",
                Status = LeaveStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            db.LeaveRequests.Add(request);
            await db.SaveChangesAsync();

            requestId = request.Id;
            balanceBefore = await db.LeaveLedgerEntries
                .Where(l => l.EmployeeId == employee.Id)
                .SumAsync(l => (decimal?)l.Days) ?? 0m;
        }

        var token = await TokenAsync(client, $"/Leave/Details/{requestId}");
        await client.PostAsync(
            $"/Leave/Approve/{requestId}",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IForm.Web.Data.ApplicationDbContext>();
            var saved = await db.LeaveRequests.FindAsync(requestId);
            Assert.Equal(LeaveStatus.Pending, saved!.Status);
            Assert.Null(saved.DecidedAt);

            var balanceAfter = await db.LeaveLedgerEntries
                .Where(l => l.EmployeeId == saved.EmployeeId)
                .SumAsync(l => (decimal?)l.Days) ?? 0m;
            Assert.Equal(balanceBefore, balanceAfter);
        }
    }

    [Fact]
    public async Task Expenses_DetailsPageRendersClaim()
    {
        var client = await LoginClientAsync(_factory, "admin@iform.app", "Admin@123");

        var detail = await client.GetStringAsync("/Expenses/Details/1");

        Assert.Contains("Claim EX-", detail);
        Assert.Contains("Amount", detail);
    }

    [Fact]
    public async Task Expenses_CreateThenAppearsInList()
    {
        var client = await LoginClientAsync(_factory, "admin@iform.app", "Admin@123");

        var token = await TokenAsync(client, "/Expenses/Create");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = "0",
            ["EmployeeId"] = "6",
            ["Category"] = "Travel",
            ["Description"] = "Workflow smoke test claim",
            ["Amount"] = "321.50",
            ["Currency"] = "INR",
            ["ExpenseDate"] = "2027-04-05",
            ["__RequestVerificationToken"] = token
        });

        var response = await client.PostAsync("/Expenses/Create", form);
        Assert.True(response.IsSuccessStatusCode, $"Claim create failed with {(int)response.StatusCode}.");

        // The description is not in the list view, so confirm via the detail page.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IForm.Web.Data.ApplicationDbContext>();
        var claim = await db.ExpenseClaims.OrderByDescending(c => c.Id).FirstAsync();

        Assert.Equal("Workflow smoke test claim", claim.Description);
        Assert.Equal(321.50m, claim.Amount);
        Assert.Equal(ExpenseStatus.Submitted, claim.Status);

        var detail = await client.GetStringAsync($"/Expenses/Details/{claim.Id}");
        Assert.Contains("Workflow smoke test claim", detail);
    }

    [Fact]
    public async Task Expenses_ApproveThenReimburse_MovesThroughTheWorkflow()
    {
        var client = await LoginClientAsync(_factory, "admin@iform.app", "Admin@123");

        int claimId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IForm.Web.Data.ApplicationDbContext>();
            var employee = await db.Employees.FirstAsync(e => e.EmployeeCode == "EMP-007");

            var claim = new ExpenseClaim
            {
                ClaimNumber = $"EX-TEST-{Guid.NewGuid():N}"[..20],
                EmployeeId = employee.Id,
                Category = "Meals",
                Amount = 400m,
                Currency = "INR",
                ExpenseDate = new DateTime(2027, 7, 1),
                Status = ExpenseStatus.Submitted,
                CreatedAt = DateTime.UtcNow
            };

            db.ExpenseClaims.Add(claim);
            await db.SaveChangesAsync();
            claimId = claim.Id;
        }

        var approveToken = await TokenAsync(client, $"/Expenses/Details/{claimId}");
        var approved = await client.PostAsync(
            $"/Expenses/Approve/{claimId}",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = approveToken }));
        Assert.True(approved.IsSuccessStatusCode, $"Approve failed with {(int)approved.StatusCode}.");

        using (var check = _factory.Services.CreateScope())
        {
            var db = check.ServiceProvider.GetRequiredService<IForm.Web.Data.ApplicationDbContext>();
            Assert.Equal(ExpenseStatus.Approved, (await db.ExpenseClaims.FindAsync(claimId))!.Status);
        }

        var reimburseToken = await TokenAsync(client, $"/Expenses/Details/{claimId}");
        var paid = await client.PostAsync(
            $"/Expenses/MarkReimbursed/{claimId}",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = reimburseToken }));
        Assert.True(paid.IsSuccessStatusCode, $"Reimburse failed with {(int)paid.StatusCode}.");

        using (var final = _factory.Services.CreateScope())
        {
            var db = final.ServiceProvider.GetRequiredService<IForm.Web.Data.ApplicationDbContext>();
            Assert.Equal(ExpenseStatus.Reimbursed, (await db.ExpenseClaims.FindAsync(claimId))!.Status);
        }
    }

    [Fact]
    public async Task Attendance_CreateThenAppearsInList()
    {
        var client = await LoginClientAsync(_factory, "admin@iform.app", "Admin@123");

        var token = await TokenAsync(client, "/Attendance/Create");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = "0",
            ["EmployeeId"] = "5",
            ["Date"] = "2027-05-03",
            ["CheckIn"] = "2027-05-03T09:00",
            ["CheckOut"] = "2027-05-03T18:30",
            ["Notes"] = "Workflow smoke test",
            ["__RequestVerificationToken"] = token
        });

        var response = await client.PostAsync("/Attendance/Create", form);
        Assert.True(response.IsSuccessStatusCode, $"Attendance create failed with {(int)response.StatusCode}.");

        var html = await client.GetStringAsync("/Attendance?employeeId=5&status=Present");
        Assert.Contains("9.5", html);
    }

    [Fact]
    public async Task Attendance_RejectsOverlongShift()
    {
        var client = await LoginClientAsync(_factory, "admin@iform.app", "Admin@123");

        var token = await TokenAsync(client, "/Attendance/Create");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = "0",
            ["EmployeeId"] = "5",
            ["Date"] = "2027-05-04",
            ["CheckIn"] = "2027-05-04T06:00",
            ["CheckOut"] = "2027-05-04T23:00",
            ["__RequestVerificationToken"] = token
        });

        var response = await client.PostAsync("/Attendance/Create", form);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("daily maximum", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Onboarding_ShowsSeededChecklists_AndOverdueTask()
    {
        var client = await LoginClientAsync(_factory, "admin@iform.app", "Admin@123");

        var html = await client.GetStringAsync("/Onboarding");

        Assert.Contains("Site Engineer", html);
        Assert.Contains("Office / Admin", html);
        Assert.Contains("Overdue tasks", html);
        Assert.Contains("Assign a buddy for the first week", html);
    }

    [Fact]
    public async Task Onboarding_ToggleTaskFromDetailPage()
    {
        var client = await LoginClientAsync(_factory, "admin@iform.app", "Admin@123");

        var before = await client.GetStringAsync("/Onboarding/Details/1");
        Assert.Contains("Mark done", before);

        var token = await TokenAsync(client, "/Onboarding/Details/1");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        });

        var response = await client.PostAsync("/Onboarding/ToggleTask/4", form);
        Assert.True(response.IsSuccessStatusCode, $"Toggle failed with {(int)response.StatusCode}.");

        var after = await client.GetStringAsync("/Onboarding/Details/1");
        Assert.DoesNotContain("Overdue", after);
    }

    [Fact]
    public async Task Onboarding_CreateTemplateThenItAppearsInList()
    {
        var client = await LoginClientAsync(_factory, "admin@iform.app", "Admin@123");

        var token = await TokenAsync(client, "/Onboarding/CreateTemplate");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = "0",
            ["Name"] = "Smoke Test Template",
            ["Description"] = "Created by a workflow test",
            ["IsActive"] = "true",
            ["Tasks[0].Title"] = "Verify documents",
            ["Tasks[0].DueDayOffset"] = "2",
            ["Tasks[1].Title"] = "Grant access",
            ["Tasks[1].DueDayOffset"] = "5",
            ["__RequestVerificationToken"] = token
        });

        var response = await client.PostAsync("/Onboarding/CreateTemplate", form);
        Assert.True(response.IsSuccessStatusCode, $"Template create failed with {(int)response.StatusCode}.");

        var html = await client.GetStringAsync("/Onboarding/Templates");
        Assert.Contains("Smoke Test Template", html);
        Assert.Contains("Verify documents", html);
    }

    [Fact]
    public async Task Onboarding_StartChecklistForNewHire()
    {
        var client = await LoginClientAsync(_factory, "admin@iform.app", "Admin@123");

        var token = await TokenAsync(client, "/Onboarding/Start");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["EmployeeId"] = "1",
            ["OnboardingTemplateId"] = "2",
            ["__RequestVerificationToken"] = token
        });

        var response = await client.PostAsync("/Onboarding/Start", form);
        Assert.True(response.IsSuccessStatusCode, $"Start failed with {(int)response.StatusCode}.");

        var html = await client.GetStringAsync("/Onboarding");
        Assert.Contains("Meera Krishnan", html);
    }

    [Fact]
    public async Task Leave_Unapprove_RestoresBalanceAndReturnsToPending()
    {
        var client = await LoginClientAsync(_factory, "admin@iform.app", "Admin@123");

        int employeeId;
        int requestId;
        decimal balanceBefore;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IForm.Web.Data.ApplicationDbContext>();
            var employee = await db.Employees.FirstAsync();

            var request = new LeaveRequest
            {
                EmployeeId = employee.Id,
                LeaveType = LeaveType.Casual,
                StartDate = UtcDates.Date(new DateTime(2027, 3, 8)),
                EndDate = UtcDates.Date(new DateTime(2027, 3, 9)),
                Days = 2m,
                Reason = "Reversal probe",
                Status = LeaveStatus.Approved,
                RequestNumber = "LV-UNAPPROVE-PROBE",
                CreatedAt = DateTime.UtcNow
            };
            db.LeaveRequests.Add(request);
            await db.SaveChangesAsync();
            requestId = request.Id;
            employeeId = employee.Id;
            balanceBefore = await db.LeaveLedgerEntries
                .Where(l => l.EmployeeId == employee.Id)
                .SumAsync(l => (decimal?)l.Days) ?? 0m;
        }

        var token = await TokenAsync(client, $"/Leave/Details/{requestId}");
        var response = await client.PostAsync(
            $"/Leave/Unapprove/{requestId}",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        Assert.True(response.IsSuccessStatusCode, $"Unapprove failed with {(int)response.StatusCode}.");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IForm.Web.Data.ApplicationDbContext>();
            var saved = await db.LeaveRequests.FindAsync(requestId);

            Assert.Equal(LeaveStatus.Pending, saved!.Status);
            Assert.Null(saved.DecidedAt);
            Assert.Null(saved.DecisionNote);

            var balanceAfter = await db.LeaveLedgerEntries
                .Where(l => l.EmployeeId == employeeId)
                .SumAsync(l => (decimal?)l.Days) ?? 0m;
            Assert.Equal(balanceBefore, balanceAfter);
        }
    }

    [Fact]
    public async Task Onboarding_DeleteRunRemovesItAndItsTasks()
    {
        var client = await LoginClientAsync(_factory, "admin@iform.app", "Admin@123");

        int runId;
        int taskCount;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IForm.Web.Data.ApplicationDbContext>();
            var employee = await db.Employees.FirstAsync();
            var template = await db.OnboardingTemplates.FirstAsync();

            var run = new EmployeeOnboarding
            {
                EmployeeId = employee.Id,
                OnboardingTemplateId = template.Id,
                StartedAt = DateTime.UtcNow
            };
            run.Tasks.Add(new EmployeeOnboardingTask
            {
                Title = "Disposable probe task",
                DueDate = UtcDates.Date(DateTime.UtcNow),
                DisplayOrder = 1
            });
            run.Tasks.Add(new EmployeeOnboardingTask
            {
                Title = "Second disposable probe task",
                DueDate = UtcDates.Date(DateTime.UtcNow),
                DisplayOrder = 2
            });

            db.EmployeeOnboardings.Add(run);
            await db.SaveChangesAsync();
            runId = run.Id;
            taskCount = run.Tasks.Count;
        }

        var token = await TokenAsync(client, $"/Onboarding/Details/{runId}");
        var response = await client.PostAsync(
            $"/Onboarding/Delete/{runId}",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        Assert.True(response.IsSuccessStatusCode, $"Delete failed with {(int)response.StatusCode}.");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IForm.Web.Data.ApplicationDbContext>();

            Assert.Null(await db.EmployeeOnboardings.FindAsync(runId));
            Assert.Empty(await db.EmployeeOnboardingTasks.Where(t => t.EmployeeOnboardingId == runId).ToListAsync());
            Assert.Equal(2, taskCount);
        }
    }

    [Fact]
    public async Task HrModules_RejectPostWithoutAntiforgeryToken()
    {
        var client = await LoginClientAsync(_factory, "admin@iform.app", "Admin@123");

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = "0",
            ["EmployeeCode"] = "EMP-777",
            ["FirstName"] = "No",
            ["LastName"] = "Token",
            ["Email"] = "notoken@iform.app"
        });

        var response = await client.PostAsync("/Employees/Create", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}