using System.ComponentModel.DataAnnotations;
using IForm.Web.Models;

namespace IForm.Web.ViewModels;

public class EmployeeListViewModel
{
    public List<Employee> Employees { get; set; } = [];

    public List<Department> Departments { get; set; } = [];

    public string? Search { get; set; }

    public int? DepartmentId { get; set; }

    public EmploymentStatus? Status { get; set; }

    public int TotalCount { get; set; }

    public int ActiveCount { get; set; }

    public int ExitedCount { get; set; }
}

public class EmployeeFormViewModel
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

    [MaxLength(120)]
    public string? JobTitle { get; set; }

    public int? ManagerId { get; set; }

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

    public List<Department> Departments { get; set; } = [];

    /// <summary>Populated by the controller to keep the manager dropdown from offering a reporting cycle.</summary>
    public List<Employee> Managers { get; set; } = [];
}