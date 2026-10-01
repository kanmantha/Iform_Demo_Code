using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IForm.Tests;

public class WebAppSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WebAppSmokeTests()
    {
        var tempDb = Path.Combine(Path.GetTempPath(), $"iform-smoke-{Guid.NewGuid():N}.db");
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={tempDb}");
            });
    }

    [Fact]
    public async Task LoginPage_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Account/Login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Sign in", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Home_ShowsLandingPageForAnonymous()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_RequiresAuthentication()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Dashboard");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SiteQueryPdf_RequiresAuthentication()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/SiteQueries/Pdf/1");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StaticAssets_AreServed()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/css/site.css");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task UserSidebar_ShowsOnlySiteQueriesSection()
    {
        var client = _factory.CreateClient();
        await LoginAsync(client, "john@iform.app", "User@123");

        var response = await client.GetAsync("/SiteQueries");
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Contains("Site Queries", html);
        Assert.Contains("Report Issue", html);
        Assert.Contains("Product Lookup", html);

        Assert.DoesNotContain("nav-section-label\">Main", html);
        Assert.DoesNotContain("All Tickets", html);
        Assert.DoesNotContain(">Dashboard<", html);
        Assert.DoesNotContain("EOT Tracker", html);
        Assert.DoesNotContain("Dispatch Tracker", html);
        Assert.DoesNotContain("Mill Test Certificates", html);
        Assert.DoesNotContain("nav-section-label\">Administration", html);
    }

    [Fact]
    public async Task AdminSidebar_KeepsAllSections()
    {
        var client = _factory.CreateClient();
        await LoginAsync(client, "admin@iform.app", "Admin@123");

        var response = await client.GetAsync("/Dashboard");
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Contains("nav-section-label\">Main", html);
        Assert.Contains("All Tickets", html);
        Assert.Contains("EOT Tracker", html);
        Assert.Contains("Dispatch Tracker", html);
        Assert.Contains("Mill Test Certificates", html);
        Assert.Contains("nav-section-label\">Administration", html);
        Assert.Contains("Site Queries", html);
    }

    [Fact]
    public async Task ReportIssuePage_RendersProductImagePicker()
    {
        var client = _factory.CreateClient();
        await LoginAsync(client, "john@iform.app", "User@123");

        var response = await client.GetAsync("/SiteQueries/Create");
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Contains("Report a Site Query", html);
        Assert.Contains("product-select", html);
        Assert.Contains("/images/product-placeholder.svg", html);
        Assert.Contains("Verified product code", html);
    }

    [Fact]
    public async Task QueryDetailsPage_RendersProductImage()
    {
        var client = _factory.CreateClient();
        await LoginAsync(client, "manager@iform.app", "Manager@123");

        var response = await client.GetAsync("/SiteQueries/Details/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Verified product", html);
        Assert.Contains("product-thumb", html);
    }
}
