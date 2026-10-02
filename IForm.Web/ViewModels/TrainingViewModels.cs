using System.ComponentModel.DataAnnotations;
using IForm.Web.Models;

namespace IForm.Web.ViewModels;

public class TrainingListViewModel
{
    public List<Training> Trainings { get; set; } = [];

    public TrainingStatus? Status { get; set; }

    public TrainingType? Type { get; set; }

    public string? Search { get; set; }

    public int TotalCount { get; set; }
}

public class TrainingFormViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public TrainingType Type { get; set; } = TrainingType.Optional;

    public TrainingStatus Status { get; set; } = TrainingStatus.NotStarted;

    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    public int? TrainerEmployeeId { get; set; }

    public List<Employee> Trainers { get; set; } = [];
}
