using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class Ticket
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string TicketNumber { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public TicketCategory Category { get; set; }

    public TicketPriority Priority { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.New;

    public string? Location { get; set; }

    public int? SiteId { get; set; }

    public DateTime? DueDate { get; set; }

    public string? ResolutionNotes { get; set; }

    public string ReportedById { get; set; } = string.Empty;

    public string? AssignedToId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public AppUser? ReportedBy { get; set; }

    public AppUser? AssignedTo { get; set; }

    public Site? Site { get; set; }

    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();

    public ICollection<ActionItem> Actions { get; set; } = new List<ActionItem>();
}
