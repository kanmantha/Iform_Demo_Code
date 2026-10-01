using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class ExpenseClaim
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string ClaimNumber { get; set; } = string.Empty;

    public int EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    [Required, MaxLength(120)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    [Range(0.01, 10_000_000)]
    public decimal Amount { get; set; }

    /// <summary>Currency code, three letters. Kept explicit so mixed-currency reports are possible later.</summary>
    [Required, MaxLength(3), RegularExpression("^[A-Za-z]{3}$")]
    public string Currency { get; set; } = "INR";

    [DataType(DataType.Date)]
    public DateTime ExpenseDate { get; set; }

    public ExpenseStatus Status { get; set; } = ExpenseStatus.Submitted;

    [MaxLength(400)]
    public string? DecisionNote { get; set; }

    public string? DecidedById { get; set; }

    public DateTime? DecidedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}