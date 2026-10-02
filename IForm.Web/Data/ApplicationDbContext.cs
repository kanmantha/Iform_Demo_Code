using IForm.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Data;

public class ApplicationDbContext : IdentityDbContext<AppUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<TicketComment> TicketComments => Set<TicketComment>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Site> Sites => Set<Site>();

    public DbSet<OrganizationUnit> OrganizationUnits => Set<OrganizationUnit>();

    public DbSet<Incident> Incidents => Set<Incident>();

    public DbSet<ActionItem> ActionItems => Set<ActionItem>();

    public DbSet<SiteQuery> SiteQueries => Set<SiteQuery>();

    public DbSet<SiteQueryPhoto> SiteQueryPhotos => Set<SiteQueryPhoto>();

    public DbSet<QueryComment> QueryComments => Set<QueryComment>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<EotRequest> EotRequests => Set<EotRequest>();

    public DbSet<DispatchOrder> DispatchOrders => Set<DispatchOrder>();

    public DbSet<MaterialCertificate> MaterialCertificates => Set<MaterialCertificate>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    public DbSet<LeaveLedgerEntry> LeaveLedgerEntries => Set<LeaveLedgerEntry>();

    public DbSet<ExpenseClaim> ExpenseClaims => Set<ExpenseClaim>();

    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    public DbSet<OnboardingTemplate> OnboardingTemplates => Set<OnboardingTemplate>();

    public DbSet<OnboardingTaskTemplate> OnboardingTaskTemplates => Set<OnboardingTaskTemplate>();

    public DbSet<EmployeeOnboarding> EmployeeOnboardings => Set<EmployeeOnboarding>();

    public DbSet<EmployeeOnboardingTask> EmployeeOnboardingTasks => Set<EmployeeOnboardingTask>();


    public DbSet<PerformanceReview> PerformanceReviews => Set<PerformanceReview>();


    public DbSet<Payslip> Payslips => Set<Payslip>();


    public DbSet<EngagementSurvey> EngagementSurveys => Set<EngagementSurvey>();


    public DbSet<EngagementResponse> EngagementResponses => Set<EngagementResponse>();


    public DbSet<Training> Trainings => Set<Training>();


    public DbSet<TrainingEnrollment> TrainingEnrollments => Set<TrainingEnrollment>();


    public DbSet<LmsCourse> LmsCourses => Set<LmsCourse>();


    public DbSet<LmsEnrollment> LmsEnrollments => Set<LmsEnrollment>();


    public DbSet<Project> Projects => Set<Project>();


    public DbSet<ProjectAssignment> ProjectAssignments => Set<ProjectAssignment>();


    public DbSet<Timesheet> Timesheets => Set<Timesheet>();


    public DbSet<Document> Documents => Set<Document>();


    public DbSet<Policy> Policies => Set<Policy>();


    public DbSet<DirectoryEntry> DirectoryEntries => Set<DirectoryEntry>();





    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Ticket>(e =>
        {
            e.HasOne(t => t.ReportedBy)
                .WithMany()
                .HasForeignKey(t => t.ReportedById)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.AssignedTo)
                .WithMany()
                .HasForeignKey(t => t.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(t => t.Site)
                .WithMany()
                .HasForeignKey(t => t.SiteId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(t => t.TicketNumber).IsUnique();
            e.HasIndex(t => t.Status);
            e.HasIndex(t => t.CreatedAt);
        });

        builder.Entity<TicketComment>(e =>
        {
            e.HasOne(c => c.Ticket)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<OrganizationUnit>(e =>
        {
            e.HasOne(u => u.Parent)
                .WithMany(u => u.Children)
                .HasForeignKey(u => u.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(u => u.Manager)
                .WithMany()
                .HasForeignKey(u => u.ManagerId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(u => u.Code).IsUnique();
        });

        builder.Entity<AppUser>(e =>
        {
            e.HasOne(u => u.OrganizationUnit)
                .WithMany(u => u.Members)
                .HasForeignKey(u => u.OrganizationUnitId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Incident>(e =>
        {
            e.HasOne(i => i.ReportedBy)
                .WithMany()
                .HasForeignKey(i => i.ReportedById)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(i => i.AssignedTo)
                .WithMany()
                .HasForeignKey(i => i.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(i => i.Site)
                .WithMany()
                .HasForeignKey(i => i.SiteId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(i => i.IncidentNumber).IsUnique();
            e.HasIndex(i => i.Status);
            e.HasIndex(i => i.OccurredAt);
        });

        builder.Entity<ActionItem>(e =>
        {
            e.HasOne(a => a.AssignedTo)
                .WithMany()
                .HasForeignKey(a => a.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.CreatedBy)
                .WithMany()
                .HasForeignKey(a => a.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Incident)
                .WithMany(i => i.Actions)
                .HasForeignKey(a => a.IncidentId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(a => a.Ticket)
                .WithMany(t => t.Actions)
                .HasForeignKey(a => a.TicketId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(a => a.ActionNumber).IsUnique();
            e.HasIndex(a => a.Status);
            e.HasIndex(a => a.DueDate);
        });

        builder.Entity<SiteQuery>(e =>
        {
            e.HasOne(q => q.RaisedBy)
                .WithMany()
                .HasForeignKey(q => q.RaisedById)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(q => q.ResolvedBy)
                .WithMany()
                .HasForeignKey(q => q.ResolvedById)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(q => q.Product)
                .WithMany(p => p.SiteQueries)
                .HasForeignKey(q => q.ProductId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(q => q.QueryNumber).IsUnique();
            e.HasIndex(q => q.IpoNumber);
            e.HasIndex(q => q.Project);
            e.HasIndex(q => q.Status);
            e.HasIndex(q => q.CreatedAt);
        });

        builder.Entity<SiteQueryPhoto>(e =>
        {
            e.HasOne(p => p.SiteQuery)
                .WithMany(q => q.Photos)
                .HasForeignKey(p => p.SiteQueryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<QueryComment>(e =>
        {
            e.HasOne(c => c.SiteQuery)
                .WithMany(q => q.Comments)
                .HasForeignKey(c => c.SiteQueryId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Product>(e =>
        {
            e.HasIndex(p => p.ProductCode).IsUnique();
        });

        builder.Entity<Notification>(e =>
        {
            e.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(n => n.SiteQuery)
                .WithMany()
                .HasForeignKey(n => n.SiteQueryId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(n => new { n.UserId, n.IsRead });
            e.HasIndex(n => n.CreatedAt);
        });

        builder.Entity<EotRequest>(e =>
        {
            e.HasOne(r => r.CreatedBy)
                .WithMany()
                .HasForeignKey(r => r.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(r => r.EotNumber).IsUnique();
            e.HasIndex(r => r.Project);
            e.HasIndex(r => r.Category);
            e.HasIndex(r => r.SubmissionStatus);
            e.HasIndex(r => r.CreatedAt);
        });

        builder.Entity<DispatchOrder>(e =>
        {
            e.HasOne(d => d.CreatedBy)
                .WithMany()
                .HasForeignKey(d => d.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(d => d.DispatchNumber).IsUnique();
            e.HasIndex(d => d.IpoNumber);
            e.HasIndex(d => d.Project);
            e.HasIndex(d => d.DispatchStatus);
            e.HasIndex(d => d.CreatedAt);
        });

        builder.Entity<MaterialCertificate>(e =>
        {
            e.HasOne(c => c.UploadedBy)
                .WithMany()
                .HasForeignKey(c => c.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(c => c.CertificateNumber).IsUnique();
            e.HasIndex(c => c.Supplier);
            e.HasIndex(c => c.CreatedAt);
        });

        builder.Entity<Department>(e =>
        {
            e.HasIndex(d => d.Code).IsUnique();
        });

        builder.Entity<Employee>(e =>
        {
            e.HasOne(x => x.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Restrict rather than cascade: deleting a manager must not silently
            // delete the people reporting to them.
            e.HasOne(x => x.Manager)
                .WithMany(x => x.DirectReports)
                .HasForeignKey(x => x.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.EmployeeCode).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.LastName);
            e.HasIndex(x => x.Status);
        });

        builder.Entity<LeaveRequest>(e =>
        {
            e.HasOne(r => r.Employee)
                .WithMany()
                .HasForeignKey(r => r.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(r => r.RequestNumber).IsUnique();
            e.HasIndex(r => r.Status);
            e.HasIndex(r => new { r.EmployeeId, r.StartDate });
        });

        builder.Entity<LeaveLedgerEntry>(e =>
        {
            e.HasOne(l => l.Employee)
                .WithMany()
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Deleting the request that consumed the days would leave the ledger
            // unbalanced, so keep the entry and the reference is cleared instead.
            e.HasOne(l => l.LeaveRequest)
                .WithMany()
                .HasForeignKey(l => l.LeaveRequestId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(l => new { l.EmployeeId, l.LeaveType });
        });

        builder.Entity<ExpenseClaim>(e =>
        {
            e.HasOne(c => c.Employee)
                .WithMany()
                .HasForeignKey(c => c.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(c => c.ClaimNumber).IsUnique();
            e.HasIndex(c => c.Status);
            e.HasIndex(c => new { c.EmployeeId, c.Status });
        });

        builder.Entity<AttendanceRecord>(e =>
        {
            e.HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // One record per employee per day, enforced by the database.
            e.HasIndex(a => new { a.EmployeeId, a.Date }).IsUnique();
            e.HasIndex(a => a.Date);
        });

        builder.Entity<OnboardingTemplate>(e =>
        {
            e.HasIndex(t => t.Name).IsUnique();
        });

        builder.Entity<OnboardingTaskTemplate>(e =>
        {
            e.HasOne(t => t.OnboardingTemplate)
                .WithMany(x => x.TaskTemplates)
                .HasForeignKey(t => t.OnboardingTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<EmployeeOnboarding>(e =>
        {
            e.HasOne(o => o.Employee)
                .WithMany()
                .HasForeignKey(o => o.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(o => o.OnboardingTemplate)
                .WithMany()
                .HasForeignKey(o => o.OnboardingTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(o => new { o.EmployeeId, o.OnboardingTemplateId }).IsUnique();
        });

        builder.Entity<EmployeeOnboardingTask>(e =>
        {
            e.HasOne(t => t.EmployeeOnboarding)
                .WithMany(o => o.Tasks)
                .HasForeignKey(t => t.EmployeeOnboardingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Employee>(e =>
        {
            e.HasOne(x => x.AppUser)
                .WithMany()
                .HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.AppUserId).IsUnique();
        });
    }
}