using System.Security.Claims;
using IForm.Web.Data;
using IForm.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace IForm.Tests;

internal static class TestData
{
    public static AppUser User(string id = "u1", string name = "John Carter", string email = "john@iform.app")
        => new()
        {
            Id = id,
            UserName = email,
            Email = email,
            FullName = name,
            Department = "Maintenance",
            JobTitle = "Engineer"
        };

    public static (ApplicationDbContext Db, SqliteConnection Connection) CreateDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return (db, connection);
    }

    public static ClaimsPrincipal Principal(AppUser user, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new("fullName", user.FullName)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }

    public static UserManager<AppUser> UserManager(AppUser user)
    {
        var store = new Mock<IUserStore<AppUser>>();
        store.Setup(s => s.FindByIdAsync(user.Id, CancellationToken.None)).ReturnsAsync(user);
        store.Setup(s => s.GetUserIdAsync(user, CancellationToken.None)).ReturnsAsync(user.Id);
        store.Setup(s => s.GetUserNameAsync(user, CancellationToken.None)).ReturnsAsync(user.UserName);

        return new UserManager<AppUser>(
            store.Object,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<AppUser>(),
            Array.Empty<IUserValidator<AppUser>>(),
            Array.Empty<IPasswordValidator<AppUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<AppUser>>.Instance);
    }

    public static async Task SeedUsersAsync(ApplicationDbContext db, params AppUser[] users)
    {
        foreach (var user in users)
        {
            if (!await db.Users.AnyAsync(u => u.Id == user.Id))
            {
                db.Users.Add(user);
            }
        }

        await db.SaveChangesAsync();
    }

    public static Ticket Ticket(int id, string number, TicketStatus status, TicketPriority priority, string reportedById, string? assignedToId = null)
        => new()
        {
            Id = id,
            TicketNumber = number,
            Title = $"Ticket {number}",
            Description = "Test ticket",
            Category = TicketCategory.General,
            Priority = priority,
            Status = status,
            ReportedById = reportedById,
            AssignedToId = assignedToId,
            CreatedAt = DateTime.UtcNow.AddDays(-id)
        };

    public static SiteQuery SiteQuery(int id, string number, QueryStatus status, string raisedById, QueryCategory category = QueryCategory.Missing)
        => new()
        {
            Id = id,
            QueryNumber = number,
            IpoNumber = $"IPO-{id}",
            Project = "Hallmark",
            Category = category,
            Status = status,
            Description = "Item missing on site",
            QuantityNos = 4,
            QuantitySqm = 2.5m,
            RaisedById = raisedById,
            CreatedAt = DateTime.UtcNow.AddDays(-id)
        };

    public static EotRequest Eot(int id, string number, string createdById, EotCategory category = EotCategory.DesignRevision)
        => new()
        {
            Id = id,
            EotNumber = number,
            Project = "Hallmark",
            Client = "Hallmark Developers",
            FinancialYear = "2026-27",
            Category = category,
            Scenario = EotScenario.Sc2,
            ChangeProposedBy = ChangeProposedBy.Architect,
            Reason = "Client requested revised facade panel layout.",
            Reference = "DR-2026-042",
            EstimatedTimeImpactDays = 14,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow.AddDays(-id)
        };

    public static DispatchOrder Dispatch(int id, string number, string project, string createdById, DispatchStatus status = DispatchStatus.Pending)
        => new()
        {
            Id = id,
            DispatchNumber = number,
            IpoNumber = $"IPO-{id}",
            Project = project,
            SlabTargetDate = DateTime.UtcNow.AddDays(-30),
            SlabCompletedDate = DateTime.UtcNow.AddDays(-20),
            SlabDelayDays = 10,
            QuantityNos = 25,
            QuantitySqm = 12.5m,
            DispatchStatus = status,
            DelayDays = 5,
            Reason = QueryCategory.Missing,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow.AddDays(-id)
        };

    public static MaterialCertificate Certificate(int id, string number, string supplier, string uploadedById)
        => new()
        {
            Id = id,
            CertificateNumber = number,
            Supplier = supplier,
            SupplierReportNo = $"R22u{id}",
            WorkOrderNo = $"JSA.26-{id:D5}",
            Alloy = "Aluminium 6063",
            TestMethod = "ASTM B221 / B557",
            ReportDate = DateTime.UtcNow.AddDays(-10),
            ValidUntil = DateTime.UtcNow.AddDays(180),
            UploadedById = uploadedById,
            CreatedAt = DateTime.UtcNow.AddDays(-id)
        };
}
