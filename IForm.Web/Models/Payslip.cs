using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class Payslip
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    [Required, MaxLength(10)]
    public string Month { get; set; } = string.Empty; // e.g., "2026-10"
    public int Year { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal Deductions { get; set; }
    public decimal NetSalary { get; set; }
    public PayslipStatus Status { get; set; } = PayslipStatus.Draft;
    public DateTime? GeneratedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
