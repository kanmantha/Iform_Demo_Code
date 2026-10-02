namespace IForm.Web.Models;

public class LmsEnrollment
{
    public int Id { get; set; }
    public int LmsCourseId { get; set; }
    public LmsCourse? LmsCourse { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public LmsStatus Status { get; set; } = LmsStatus.NotStarted;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
