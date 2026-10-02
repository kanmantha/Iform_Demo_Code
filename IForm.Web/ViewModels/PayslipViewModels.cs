using System.ComponentModel.DataAnnotations;
using IForm.Web.Models;

namespace IForm.Web.ViewModels;

public class MyPayslipListViewModel
{
    public List<Payslip> Payslips { get; set; } = [];

    public PayslipStatus? Status { get; set; }

    public int? Year { get; set; }

    public int? Month { get; set; }

    public int TotalCount { get; set; }
}

public class PayslipFormViewModel
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    [Required]
    [Range(2000, 2100)]
    public int Year { get; set; } = DateTime.UtcNow.Year;

    [Required]
    [Range(1, 12)]
    public int Month { get; set; } = DateTime.UtcNow.Month;

    public decimal BasicPay { get; set; }

    public decimal Allowances { get; set; }

    public decimal Deductions { get; set; }

    public decimal NetPay { get; set; }

    public PayslipStatus Status { get; set; } = PayslipStatus.Draft;

    public DateTime? PaidAt { get; set; }

    public List<Employee> Employees { get; set; } = [];
}
