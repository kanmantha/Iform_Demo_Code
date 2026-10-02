using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class DirectoryEntry
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public bool IsPublished { get; set; } = true;
}
