using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class ActionItem
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string ActionNumber { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public ActionPriority Priority { get; set; } = ActionPriority.Medium;

    public ActionStatus Status { get; set; } = ActionStatus.Open;

    public DateTime? DueDate { get; set; }

    public string AssignedToId { get; set; } = string.Empty;

    public int? IncidentId { get; set; }

    public int? TicketId { get; set; }

    public string CreatedById { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? CompletionNotes { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public Incident? Incident { get; set; }

    public Ticket? Ticket { get; set; }

    public AppUser? AssignedTo { get; set; }

    public AppUser? CreatedBy { get; set; }
}
