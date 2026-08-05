using IForm.Web.Models;

namespace IForm.Web.ViewModels;

public class DashboardViewModel
{
    public int TotalTickets { get; set; }

    public int OpenTickets { get; set; }

    public int InProgressTickets { get; set; }

    public int ResolvedTickets { get; set; }

    public int ClosedTickets { get; set; }

    public int CriticalTickets { get; set; }

    public double ResolvedRate { get; set; }

    public double AvgResolutionDays { get; set; }

    public int MyAssignedTickets { get; set; }

    public int MyReportedTickets { get; set; }

    public int TotalIncidents { get; set; }

    public int OpenIncidents { get; set; }

    public int CriticalIncidents { get; set; }

    public int OpenActions { get; set; }

    public int OverdueActions { get; set; }

    public int CompletedActions { get; set; }

    public int MyOpenActions { get; set; }

    public IReadOnlyDictionary<string, int> TicketsByStatus { get; set; } = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> TicketsByPriority { get; set; } = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> TicketsByCategory { get; set; } = new Dictionary<string, int>();

    public IReadOnlyList<DailyCount> TicketsPerDay { get; set; } = new List<DailyCount>();

    public IReadOnlyList<DepartmentCount> TicketsByDepartment { get; set; } = new List<DepartmentCount>();

    public IReadOnlyList<Ticket> RecentTickets { get; set; } = new List<Ticket>();

    public IReadOnlyList<AuditLog> RecentActivity { get; set; } = new List<AuditLog>();
}

public record DailyCount(string Day, int Created);

public record DepartmentCount(string Department, int Count);
