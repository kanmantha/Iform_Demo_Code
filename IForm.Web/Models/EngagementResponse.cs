using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class EngagementResponse
{
    public int Id { get; set; }
    public int EngagementSurveyId { get; set; }
    public EngagementSurvey? EngagementSurvey { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int Rating { get; set; } // 1-5
    [MaxLength(1000)]
    public string? Comments { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
