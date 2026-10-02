namespace IForm.Web.Models;

public class TrainingEnrollment
{
    public int Id { get; set; }
    public int TrainingId { get; set; }
    public Training? Training { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public TrainingStatus Status { get; set; } = TrainingStatus.NotStarted;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? Score { get; set; }
}
