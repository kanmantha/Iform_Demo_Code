using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

/// <summary>
/// One day of attendance for one employee. Hours worked is computed from
/// CheckIn and CheckOut rather than entered, so an impossible day
/// (negative hours) cannot be stored.
/// </summary>
public class AttendanceRecord
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    public DateTime? CheckIn { get; set; }

    public DateTime? CheckOut { get; set; }

    /// <summary>Hours worked, derived by the controller from check-in and check-out.</summary>
    public decimal HoursWorked { get; set; }

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    [MaxLength(300)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}