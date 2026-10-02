using System.ComponentModel.DataAnnotations;
using IForm.Web.Models;

namespace IForm.Web.ViewModels;

public class LmsListViewModel
{
    public List<LmsCourse> Courses { get; set; } = [];

    public LmsStatus? Status { get; set; }

    public string? Search { get; set; }

    public int TotalCount { get; set; }
}

public class LmsFormViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public LmsStatus Status { get; set; } = LmsStatus.NotStarted;

    public int? DurationHours { get; set; }
}
