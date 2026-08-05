using System.ComponentModel.DataAnnotations;
using IForm.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IForm.Web.ViewModels;

public class EotListViewModel
{
    public string? Search { get; set; }

    public EotCategory? Category { get; set; }

    public EotScenario? Scenario { get; set; }

    public EotSubmissionStatus? SubmissionStatus { get; set; }

    public EotClientApproval? ClientApproval { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public IReadOnlyList<EotRequest> EotRequests { get; set; } = new List<EotRequest>();

    public IEnumerable<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> ScenarioOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> SubmissionStatusOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> ClientApprovalOptions { get; set; } = new List<SelectListItem>();
}

public class EotFormViewModel
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string? EotNumber { get; set; }

    [Required, MaxLength(200), Display(Name = "Project")]
    public string Project { get; set; } = string.Empty;

    [MaxLength(200), Display(Name = "Client")]
    public string? Client { get; set; }

    [MaxLength(20), Display(Name = "Financial year")]
    public string? FinancialYear { get; set; }

    [Display(Name = "Revision number")]
    public int? RevisionNumber { get; set; }

    [Display(Name = "EOT category")]
    public EotCategory Category { get; set; }

    [Display(Name = "Scenario")]
    public EotScenario? Scenario { get; set; }

    [Display(Name = "Change proposed by")]
    public ChangeProposedBy? ChangeProposedBy { get; set; }

    [MaxLength(300), Display(Name = "Reference")]
    public string? Reference { get; set; }

    [Required, MaxLength(1000), Display(Name = "Reason")]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "SPA date")]
    public DateTime? SpaDate { get; set; }

    [Display(Name = "Design revision date")]
    public DateTime? DesignRevisionDate { get; set; }

    [Display(Name = "Estimated time impact (days)")]
    public int? EstimatedTimeImpactDays { get; set; }

    [Display(Name = "Estimated cost impact")]
    public decimal? EstimatedCostImpact { get; set; }

    [Display(Name = "Original approved scope")]
    public string? OriginalApprovedScope { get; set; }

    [Display(Name = "Revised scope")]
    public string? RevisedScope { get; set; }

    [Display(Name = "Scope addition (+)")]
    public decimal? ScopeAddition { get; set; }

    [Display(Name = "Scope reduction (-)")]
    public decimal? ScopeReduction { get; set; }

    [Display(Name = "Delay (days)")]
    public int? DelayDays { get; set; }

    [MaxLength(100), Display(Name = "Cost escalation")]
    public string? CostEscalation { get; set; }

    [Display(Name = "Submission status")]
    public EotSubmissionStatus SubmissionStatus { get; set; } = EotSubmissionStatus.Draft;

    [Display(Name = "Client approval")]
    public EotClientApproval ClientApproval { get; set; } = EotClientApproval.Pending;

    [MaxLength(500)]
    public string? Remarks { get; set; }

    [Display(Name = "Approved Drawings")]
    public bool HasApprovedDrawings { get; set; }

    [Display(Name = "Revised Drawings")]
    public bool HasRevisedDrawings { get; set; }

    [Display(Name = "Client Instructions / Emails")]
    public bool HasClientInstructions { get; set; }

    [Display(Name = "Delay Analysis Report")]
    public bool HasDelayAnalysis { get; set; }

    [Display(Name = "Scope Variation Statement")]
    public bool HasScopeVariationStatement { get; set; }

    [Display(Name = "Project Progress Report")]
    public bool HasProgressReport { get; set; }

    [Display(Name = "Consultant Correspondence")]
    public bool HasConsultantCorrespondence { get; set; }

    [Display(Name = "Supporting Documents")]
    public bool HasSupportingDocuments { get; set; }

    public IEnumerable<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> ScenarioOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> ChangeProposedByOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> SubmissionStatusOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> ClientApprovalOptions { get; set; } = new List<SelectListItem>();

    public IReadOnlyList<string> ProjectOptions { get; set; } = new List<string>();
}

public class EotDetailViewModel
{
    public EotRequest EotRequest { get; set; } = null!;

    public bool CanEdit { get; set; }
}
