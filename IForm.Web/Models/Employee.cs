using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class Employee
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(160)]
    public string Email { get; set; } = string.Empty;

    [Phone, MaxLength(30)]
    public string? Phone { get; set; }

    public int? DepartmentId { get; set; }

    public Department? Department { get; set; }

    [MaxLength(120)]
    public string? JobTitle { get; set; }

    /// <summary>Reporting line. Excludes self-reference, enforced in the controller.</summary>
    public int? ManagerId { get; set; }

    public Employee? Manager { get; set; }

    public List<Employee> DirectReports { get; set; } = [];

    public EmploymentStatus Status { get; set; } = EmploymentStatus.Active;

    [DataType(DataType.Date)]
    public DateTime? DateJoined { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(80)]
    public string? EmergencyContactName { get; set; }

    [Phone, MaxLength(30)]
    public string? EmergencyContactPhone { get; set; }

    [MaxLength(450)]
    public string? AppUserId { get; set; }

    public AppUser? AppUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string FullName => $"{FirstName} {LastName}".Trim();
}