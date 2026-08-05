namespace IForm.Web.Models;

public enum TicketCategory
{
    Incident,
    NearMiss,
    UnsafeAct,
    UnsafeCondition,
    Hazard,
    Equipment,
    Training,
    General,
    Other
}

public enum TicketPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum TicketStatus
{
    New,
    Open,
    InProgress,
    Pending,
    Resolved,
    Closed
}

public enum IncidentType
{
    UnsafeAct,
    UnsafeCondition,
    NearMiss,
    FirstAidCase,
    PropertyDamage,
    LostTimeInjury,
    Illness,
    Environmental
}

public enum IncidentSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum IncidentStatus
{
    New,
    UnderInvestigation,
    InvestigationComplete,
    CorrectiveAction,
    Closed
}

public enum ActionPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum ActionStatus
{
    Open,
    InProgress,
    Completed,
    Cancelled
}

public enum QueryCategory
{
    Missing,
    ProductionMistake,
    DesignMistake,
    DispatchMissing
}

public enum QueryStatus
{
    Pending,
    InProgress,
    Resolved
}

public enum DispatchStatus
{
    Pending,
    Dispatched
}

public enum EotCategory
{
    DesignRevision,
    ScopeChange,
    ClientInstruction,
    ApprovalDelay,
    SiteConstraint,
    ForceMajeure,
    OtherContractualEvents
}

public enum EotScenario
{
    Sc1,
    Sc2,
    Sc3
}

public enum ChangeProposedBy
{
    Architect,
    Structural,
    MEP,
    Client
}

public enum EotSubmissionStatus
{
    Draft,
    Submitted,
    UnderReview,
    Resubmitted,
    Approved
}

public enum EotClientApproval
{
    Pending,
    UnderReview,
    Approved,
    Rejected
}
