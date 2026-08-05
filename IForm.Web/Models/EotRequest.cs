using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class EotRequest
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string EotNumber { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Project { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Client { get; set; }

    [MaxLength(20)]
    public string? FinancialYear { get; set; }

    public int? RevisionNumber { get; set; }

    public EotCategory Category { get; set; }

    public EotScenario? Scenario { get; set; }

    public ChangeProposedBy? ChangeProposedBy { get; set; }

    [MaxLength(300)]
    public string? Reference { get; set; }

    [Required, MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    public DateTime? SpaDate { get; set; }

    public DateTime? DesignRevisionDate { get; set; }

    public int? EstimatedTimeImpactDays { get; set; }

    public decimal? EstimatedCostImpact { get; set; }

    public string? OriginalApprovedScope { get; set; }

    public string? RevisedScope { get; set; }

    public decimal? ScopeAddition { get; set; }

    public decimal? ScopeReduction { get; set; }

    public int? DelayDays { get; set; }

    [MaxLength(100)]
    public string? CostEscalation { get; set; }

    public EotSubmissionStatus SubmissionStatus { get; set; } = EotSubmissionStatus.Draft;

    public EotClientApproval ClientApproval { get; set; } = EotClientApproval.Pending;

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public bool HasApprovedDrawings { get; set; }

    public bool HasRevisedDrawings { get; set; }

    public bool HasClientInstructions { get; set; }

    public bool HasDelayAnalysis { get; set; }

    public bool HasScopeVariationStatement { get; set; }

    public bool HasProgressReport { get; set; }

    public bool HasConsultantCorrespondence { get; set; }

    public bool HasSupportingDocuments { get; set; }

    public string CreatedById { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public AppUser? CreatedBy { get; set; }
}

public sealed record EotDocumentItem(string Property, string Label, bool Mandatory);

public static class EotDocuments
{
    public static readonly IReadOnlyList<EotDocumentItem> Required = new[]
    {
        new EotDocumentItem(nameof(EotRequest.HasApprovedDrawings), "Approved Drawings", true),
        new EotDocumentItem(nameof(EotRequest.HasRevisedDrawings), "Revised Drawings", true),
        new EotDocumentItem(nameof(EotRequest.HasClientInstructions), "Client Instructions / Emails", true),
        new EotDocumentItem(nameof(EotRequest.HasDelayAnalysis), "Delay Analysis Report", true),
        new EotDocumentItem(nameof(EotRequest.HasScopeVariationStatement), "Scope Variation Statement", true),
        new EotDocumentItem(nameof(EotRequest.HasProgressReport), "Project Progress Report", true),
        new EotDocumentItem(nameof(EotRequest.HasConsultantCorrespondence), "Consultant Correspondence", true),
        new EotDocumentItem(nameof(EotRequest.HasSupportingDocuments), "Supporting Documents", false)
    };

    public static bool IsComplete(EotRequest request)
        => request.HasApprovedDrawings
        && request.HasRevisedDrawings
        && request.HasClientInstructions
        && request.HasDelayAnalysis
        && request.HasScopeVariationStatement
        && request.HasProgressReport
        && request.HasConsultantCorrespondence;
}
