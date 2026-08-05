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
    }
}
