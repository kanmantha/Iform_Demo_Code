namespace IForm.Web.Models;

public class ProjectAssignment
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
