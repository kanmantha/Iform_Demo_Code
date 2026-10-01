using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IForm.Tests;

/// <summary>
/// Renders each HR page through the real pipeline. Razor compiles at runtime, so
/// these are the only tests that catch a broken view or layout.
/// </summary>
public class HrPageSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HrPageSmokeTests()
    {
        var tempDb = Path.Combine(Path.GetTempPath(), $"iform-hr-{Guid.NewGuid():N}.db");
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={tempDb}");
            });
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var loginPage = await client.GetAsync("/Account/Login");
        var html = await loginPage.Content.ReadAsStringAsync();

        var tokenMatch = System.Text.RegularExpressions.Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(tokenMatch.Success, "Antiforgery token not found on the login page.");

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["RememberMe"] = "false",
            ["__RequestVerificationToken"] = tokenMatch.Groups[1].Value
        });

        var response = await client.PostAsync("/Account/Login", form);

        // With AllowAutoRedirect disabled a successful login answers 302; otherwise
        // it lands back on the page as 200.
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect,
            $"Unexpected login status {(int)response.StatusCode}.");
        return await response.Content.ReadAsStringAsync();
    }

    public static TheoryData<string, string> HrPages => new()
    {
        { "/Employees", "Employee Directory" },
        { "/Employees/Create", "Add Employee" },
        { "/Leave", "Leave Management" },
        { "/Leave/Create", "Request Leave" },
        { "/Expenses", "Expense Claims" },
        { "/Expenses/Create", "Submit Expense Claim" },
        { "/Attendance", "Manual Attendance" },
        { "/Attendance/Create", "Record Attendance" },
        { "/Onboarding", "Onboarding Checklists" },
        { "/Onboarding/Start", "Start Onboarding" },
        { "/Onboarding/Templates", "Onboarding Templates" },
        { "/Onboarding/CreateTemplate", "New Onboarding Template" }
    };

    [Theory]
    [MemberData(nameof(HrPages))]
    public async Task Admin_CanRenderEveryHrPage(string path, string expectedHeading)
    {
        var client = _factory.CreateClient();
        await LoginAsync(client, "admin@iform.app", "Admin@123");

        var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(expectedHeading, html);
        Assert.DoesNotContain("An unhandled exception", html);
    }

    [Theory]
    [MemberData(nameof(HrPages))]
    public async Task Manager_CanRenderEveryHrPage(string path, string expectedHeading)
    {
        var client = _factory.CreateClient();
        await LoginAsync(client, "manager@iform.app", "Manager@123");

        var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(expectedHeading, html);
    }

    [Theory]
    [InlineData("/Employees")]
    [InlineData("/Leave")]
    [InlineData("/Expenses")]
    [InlineData("/Attendance")]
    [InlineData("/Onboarding")]
    public async Task PlainUser_IsRedirectedAwayFromHrModules(string path)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await LoginAsync(client, "john@iform.app", "User@123");

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_IsRedirectedToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Employees");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HrSidebar_ShowsAllModulesForAdmin()
    {
        var client = _factory.CreateClient();
        await LoginAsync(client, "admin@iform.app", "Admin@123");

        var response = await client.GetAsync("/Employees");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("nav-section-label\">HR", html);
        Assert.Contains(">Employees<", html);
        Assert.Contains(">Leave<", html);
        Assert.Contains(">Expenses<", html);
        Assert.Contains(">Attendance<", html);
        Assert.Contains(">Onboarding<", html);
    }

    [Fact]
    public async Task HrSidebar_HidesHrSectionFromPlainUser()
    {
        var client = _factory.CreateClient();
        await LoginAsync(client, "john@iform.app", "User@123");

        var response = await client.GetAsync("/SiteQueries");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("nav-section-label\">HR", html);
        Assert.DoesNotContain("/Onboarding", html);
        Assert.DoesNotContain("/Expenses", html);
    }

    [Fact]
    public async Task Home_HrCardLinksToEmployees_ForAdmin()
    {
        var client = _factory.CreateClient();
        await LoginAsync(client, "admin@iform.app", "Admin@123");

        var response = await client.GetAsync("/Home/Home");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Employees, leave, expenses, attendance and onboarding.", html);
        Assert.Contains("href=\"/Employees\"", html);
    }

    [Fact]
    public async Task Home_HrCardHiddenFromPlainUser()
    {
        var client = _factory.CreateClient();
        await LoginAsync(client, "john@iform.app", "User@123");

        var response = await client.GetAsync("/Home/Home");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("Employees, leave, expenses, attendance and onboarding.", html);
    }
}