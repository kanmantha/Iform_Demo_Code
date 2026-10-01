using IForm.Web.Models;

namespace IForm.Web.ViewModels;

public class DashboardViewModel
{
    public int MyReportedTickets { get; set; }

    public int MyOpenTickets { get; set; }

    public int MyInProgressTickets { get; set; }

    public int MyResolvedTickets { get; set; }

    public int MyClosedTickets { get; set; }

    public int MyCriticalTickets { get; set; }

    public double ResolvedRate { get; set; }

    public double AvgResolutionDays { get; set; }

    public int MyAssignedTickets { get; set; }

    public int MyIncidents { get; set; }

    public int MyOpenIncidents { get; set; }

    public int MyCriticalIncidents { get; set; }

    public int MyOpenActions { get; set; }

    public int MyOverdueActions { get; set; }

    public int MyCompletedActions { get; set; }

    public IReadOnlyDictionary<string, int> TicketsByStatus { get; set; } = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> TicketsByPriority { get; set; } = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> TicketsByCategory { get; set; } = new Dictionary<string, int>();

    public IReadOnlyList<DailyCount> TicketsPerDay { get; set; } = new List<DailyCount>();

    public IReadOnlyList<Ticket> RecentTickets { get; set; } = new List<Ticket>();

    public IReadOnlyList<AuditLog> RecentActivity { get; set; } = new List<AuditLog>();
}

public record DailyCount(string Day, int Created);
