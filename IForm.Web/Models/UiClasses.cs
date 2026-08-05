namespace IForm.Web.Models;

public static class UiClasses
{
    public static string Status(TicketStatus status) => status switch
    {
        TicketStatus.New => "badge-status-new",
        TicketStatus.Open => "badge-status-open",
        TicketStatus.InProgress => "badge-status-progress",
        TicketStatus.Pending => "badge-status-pending",
        TicketStatus.Resolved => "badge-status-resolved",
        TicketStatus.Closed => "badge-status-closed",
        _ => "badge-secondary"
    };

    public static string StatusColor(TicketStatus status) => status switch
    {
        TicketStatus.New => "#3b82f6",
        TicketStatus.Open => "#8b5cf6",
        TicketStatus.InProgress => "#f59e0b",
        TicketStatus.Pending => "#d946ef",
        TicketStatus.Resolved => "#10b981",
        TicketStatus.Closed => "#94a3b8",
        _ => "#64748b"
    };

    public static string Priority(TicketPriority priority) => priority switch
    {
        TicketPriority.Low => "badge-priority-low",
        TicketPriority.Medium => "badge-priority-medium",
        TicketPriority.High => "badge-priority-high",
        TicketPriority.Critical => "badge-priority-critical",
        _ => "badge-secondary"
    };

    public static string Category(TicketCategory category) => category switch
    {
        TicketCategory.Incident => "badge-cat-incident",
        TicketCategory.NearMiss => "badge-cat-nearmiss",
        TicketCategory.UnsafeAct => "badge-cat-ua",
        TicketCategory.UnsafeCondition => "badge-cat-uc",
        TicketCategory.Hazard => "badge-cat-hazard",
        TicketCategory.Equipment => "badge-cat-equipment",
        TicketCategory.Training => "badge-cat-training",
        TicketCategory.General => "badge-cat-general",
        _ => "badge-secondary"
    };

    public static string PriorityColor(TicketPriority priority) => priority switch
    {
        TicketPriority.Low => "#10b981",
        TicketPriority.Medium => "#f59e0b",
        TicketPriority.High => "#f97316",
        TicketPriority.Critical => "#ef4444",
        _ => "#64748b"
    };

    public static string IncidentType(IForm.Web.Models.IncidentType type) => type switch
    {
        IForm.Web.Models.IncidentType.UnsafeAct => "badge-cat-ua",
        IForm.Web.Models.IncidentType.UnsafeCondition => "badge-cat-uc",
        IForm.Web.Models.IncidentType.NearMiss => "badge-cat-nearmiss",
        IForm.Web.Models.IncidentType.FirstAidCase => "badge-cat-incident",
        IForm.Web.Models.IncidentType.PropertyDamage => "badge-cat-hazard",
        IForm.Web.Models.IncidentType.LostTimeInjury => "badge-priority-critical",
        IForm.Web.Models.IncidentType.Illness => "badge-cat-general",
        IForm.Web.Models.IncidentType.Environmental => "badge-status-progress",
        _ => "badge-secondary"
    };

    public static string IncidentSeverity(IForm.Web.Models.IncidentSeverity severity) => severity switch
    {
        IForm.Web.Models.IncidentSeverity.Low => "badge-priority-low",
        IForm.Web.Models.IncidentSeverity.Medium => "badge-priority-medium",
        IForm.Web.Models.IncidentSeverity.High => "badge-priority-high",
        IForm.Web.Models.IncidentSeverity.Critical => "badge-priority-critical",
        _ => "badge-secondary"
    };

    public static string IncidentStatus(IForm.Web.Models.IncidentStatus status) => status switch
    {
        IForm.Web.Models.IncidentStatus.New => "badge-status-new",
        IForm.Web.Models.IncidentStatus.UnderInvestigation => "badge-status-open",
        IForm.Web.Models.IncidentStatus.InvestigationComplete => "badge-status-progress",
        IForm.Web.Models.IncidentStatus.CorrectiveAction => "badge-status-pending",
        IForm.Web.Models.IncidentStatus.Closed => "badge-status-closed",
        _ => "badge-secondary"
    };

    public static string ActionPriority(IForm.Web.Models.ActionPriority priority) => priority switch
    {
        IForm.Web.Models.ActionPriority.Low => "badge-priority-low",
        IForm.Web.Models.ActionPriority.Medium => "badge-priority-medium",
        IForm.Web.Models.ActionPriority.High => "badge-priority-high",
        IForm.Web.Models.ActionPriority.Critical => "badge-priority-critical",
        _ => "badge-secondary"
    };

    public static string ActionStatus(IForm.Web.Models.ActionStatus status) => status switch
    {
        IForm.Web.Models.ActionStatus.Open => "badge-status-new",
        IForm.Web.Models.ActionStatus.InProgress => "badge-status-progress",
        IForm.Web.Models.ActionStatus.Completed => "badge-status-resolved",
        IForm.Web.Models.ActionStatus.Cancelled => "badge-status-closed",
        _ => "badge-secondary"
    };

    public static string QueryCategory(IForm.Web.Models.QueryCategory category) => category switch
    {
        IForm.Web.Models.QueryCategory.Missing => "badge-cat-equipment",
        IForm.Web.Models.QueryCategory.ProductionMistake => "badge-cat-general",
        IForm.Web.Models.QueryCategory.DesignMistake => "badge-cat-training",
        IForm.Web.Models.QueryCategory.DispatchMissing => "badge-cat-hazard",
        _ => "badge-secondary"
    };

    public static string QueryStatus(IForm.Web.Models.QueryStatus status) => status switch
    {
        IForm.Web.Models.QueryStatus.Pending => "badge-status-new",
        IForm.Web.Models.QueryStatus.InProgress => "badge-status-progress",
        IForm.Web.Models.QueryStatus.Resolved => "badge-status-resolved",
        _ => "badge-secondary"
    };

    public static string DelayBadge(int delayDays) => delayDays switch
    {
        >= 45 => "badge-priority-critical",
        >= 30 => "badge-priority-high",
        >= 15 => "badge-priority-medium",
        _ => "badge-priority-low"
    };

    public static string EotCategory(IForm.Web.Models.EotCategory category) => category switch
    {
        IForm.Web.Models.EotCategory.DesignRevision => "badge-status-progress",
        IForm.Web.Models.EotCategory.ScopeChange => "badge-cat-equipment",
        IForm.Web.Models.EotCategory.ClientInstruction => "badge-status-open",
        IForm.Web.Models.EotCategory.ApprovalDelay => "badge-priority-high",
        IForm.Web.Models.EotCategory.SiteConstraint => "badge-cat-hazard",
        IForm.Web.Models.EotCategory.ForceMajeure => "badge-priority-critical",
        IForm.Web.Models.EotCategory.OtherContractualEvents => "badge-secondary",
        _ => "badge-secondary"
    };

    public static string EotSubmissionStatus(IForm.Web.Models.EotSubmissionStatus status) => status switch
    {
        IForm.Web.Models.EotSubmissionStatus.Draft => "badge-secondary",
        IForm.Web.Models.EotSubmissionStatus.Submitted => "badge-status-open",
        IForm.Web.Models.EotSubmissionStatus.UnderReview => "badge-status-progress",
        IForm.Web.Models.EotSubmissionStatus.Resubmitted => "badge-status-pending",
        IForm.Web.Models.EotSubmissionStatus.Approved => "badge-status-resolved",
        _ => "badge-secondary"
    };

    public static string EotClientApproval(IForm.Web.Models.EotClientApproval approval) => approval switch
    {
        IForm.Web.Models.EotClientApproval.Pending => "badge-status-pending",
        IForm.Web.Models.EotClientApproval.UnderReview => "badge-status-progress",
        IForm.Web.Models.EotClientApproval.Approved => "badge-status-resolved",
        IForm.Web.Models.EotClientApproval.Rejected => "badge-priority-critical",
        _ => "badge-secondary"
    };

    public static string Scenario(IForm.Web.Models.EotScenario scenario) => scenario switch
    {
        IForm.Web.Models.EotScenario.Sc1 => "badge-cat-general",
        IForm.Web.Models.EotScenario.Sc2 => "badge-status-progress",
        IForm.Web.Models.EotScenario.Sc3 => "badge-status-new",
        _ => "badge-secondary"
    };

    public const int EscalationThresholdDays = 30;

    public static bool IsEscalated(SiteQuery query)
        => query.Status != IForm.Web.Models.QueryStatus.Resolved && query.DelayDays >= EscalationThresholdDays;

    public static string DispatchStatus(IForm.Web.Models.DispatchStatus status) => status switch
    {
        IForm.Web.Models.DispatchStatus.Pending => "badge-status-pending",
        IForm.Web.Models.DispatchStatus.Dispatched => "badge-status-resolved",
        _ => "badge-secondary"
    };

    public static string DispatchStatusColor(IForm.Web.Models.DispatchStatus status) => status switch
    {
        IForm.Web.Models.DispatchStatus.Pending => "#d946ef",
        IForm.Web.Models.DispatchStatus.Dispatched => "#10b981",
        _ => "#64748b"
    };
}
