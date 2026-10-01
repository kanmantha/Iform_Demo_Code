using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

/// <summary>A reusable onboarding checklist definition.</summary>
public class OnboardingTemplate
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<OnboardingTaskTemplate> TaskTemplates { get; set; } = [];
}

public class OnboardingTaskTemplate
{
    public int Id { get; set; }

    public int OnboardingTemplateId { get; set; }

    public OnboardingTemplate? OnboardingTemplate { get; set; }

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [Range(1, 365)]
    public int DueDayOffset { get; set; }

    public int DisplayOrder { get; set; }
}

/// <summary>An onboarding run for one employee, created from a template.</summary>
public class EmployeeOnboarding
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public int OnboardingTemplateId { get; set; }

    public OnboardingTemplate? OnboardingTemplate { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public List<EmployeeOnboardingTask> Tasks { get; set; } = [];

    public int TotalTasks => Tasks.Count;

    public int CompletedTasks => Tasks.Count(t => t.IsCompleted);

    public decimal CompletionPercent => TotalTasks == 0 ? 0 : Math.Round(CompletedTasks * 100m / TotalTasks, 1);
}

public class EmployeeOnboardingTask
{
    public int Id { get; set; }

    public int EmployeeOnboardingId { get; set; }

    public EmployeeOnboarding? EmployeeOnboarding { get; set; }

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    public DateTime DueDate { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? CompletedById { get; set; }
}