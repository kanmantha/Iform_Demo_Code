using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class Incident
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string IncidentNumber { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public IncidentType Type { get; set; }

    public IncidentSeverity Severity { get; set; } = IncidentSeverity.Medium;

    public IncidentStatus Status { get; set; } = IncidentStatus.New;

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    [MaxLength(120)]
    public string? Location { get; set; }

    public int? SiteId { get; set; }

    public string ReportedById { get; set; } = string.Empty;

    public string? AssignedToId { get; set; }

    [MaxLength(1000)]
    public string? ImmediateActionTaken { get; set; }

    [MaxLength(1000)]
    public string? RootCause { get; set; }

    [MaxLength(2000)]
    public string? InvestigationNotes { get; set; }

    public bool IsWorkRelated { get; set; } = true;

    public int? PersonsInvolved { get; set; }

    public int? DaysLost { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public Site? Site { get; set; }

    public AppUser? ReportedBy { get; set; }

    public AppUser? AssignedTo { get; set; }

    public ICollection<ActionItem> Actions { get; set; } = new List<ActionItem>();
}
