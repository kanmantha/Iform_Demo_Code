namespace IForm.Web.Models;public enum TicketCategory{    Incident,    NearMiss,    UnsafeAct,    UnsafeCondition,    Hazard,    Equipment,    Training,    General,    Other}public enum TicketPriority{    Low,    Medium,    High,    Critical}public enum TicketStatus{    New,    Open,    InProgress,    Pending,    Resolved,    Closed}public enum IncidentType{    UnsafeAct,    UnsafeCondition,    NearMiss,    FirstAidCase,    PropertyDamage,    LostTimeInjury,    Illness,    Environmental}public enum IncidentSeverity{    Low,    Medium,    High,    Critical}public enum IncidentStatus{    New,    UnderInvestigation,    InvestigationComplete,    CorrectiveAction,    Closed}public enum ActionPriority{    Low,    Medium,    High,    Critical}public enum ActionStatus{    Open,    InProgress,    Completed,    Cancelled}public enum QueryCategory{    Missing,    ProductionMistake,    DesignMistake,    DispatchMissing}public enum QueryStatus{    Pending,    InProgress,    Resolved}public enum DispatchStatus{    Pending,    Dispatched}public enum EotCategory{    DesignRevision,    ScopeChange,    ClientInstruction,    ApprovalDelay,    SiteConstraint,    ForceMajeure,    OtherContractualEvents}public enum EotScenario{    Sc1,    Sc2,    Sc3}public enum ChangeProposedBy{    Architect,    Structural,    MEP,    Client}public enum EotSubmissionStatus{    Draft,    Submitted,    UnderReview,    Resubmitted,    Approved}public enum EotClientApproval{    Pending,    UnderReview,    Approved,    Rejected}public enum EmploymentStatus{    Active,    OnProbation,    OnLeave,    NoticePeriod,    Exited}public enum LeaveType{    Casual,    Sick,    Earned,    Unpaid,    Maternity,    CompOff}public enum LeaveStatus{    Pending,    Approved,    Rejected,    Cancelled}public enum ExpenseStatus{    Draft,    Submitted,    Approved,    Rejected,    Reimbursed}public enum AttendanceStatus{    Present,    Absent,    HalfDay,    OnLeave,    Holiday,    Weekend}// Additional enums for requested HR modules
public enum PerformanceRating
{
    Unrated,
    Poor,
    NeedsImprovement,
    MeetsExpectations,
    ExceedsExpectations,
    Outstanding
}

public enum PerformanceStatus
{
    Draft,
    InProgress,
    Completed,
    Approved
}

public enum PayslipStatus
{
    Draft,
    Generated,
    Sent,
    Paid
}

public enum EngagementStatus
{
    Draft,
    Open,
    Closed,
    Archived
}

public enum TrainingStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

public enum TrainingType
{
    Mandatory,
    Optional,
    Compliance
}

public enum LmsStatus
{
    NotStarted,
    InProgress,
    Completed
}

public enum ProjectStatus
{
    Planned,
    Active,
    OnHold,
    Completed,
    Cancelled
}

public enum TimesheetStatus
{
    Draft,
    Submitted,
    Approved,
    Rejected
}

public enum DocumentCategory
{
    General,
    HR,
    Policy,
    Training,
    Performance,
    Payslip
}

public enum DocumentVisibility
{
    Public,
    Internal,
    Private,
    EmployeeOnly
}

public enum PolicyStatus
{
    Draft,
    Published,
    Archived
}
