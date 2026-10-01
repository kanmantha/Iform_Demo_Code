using System.ComponentModel.DataAnnotations;
using IForm.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IForm.Web.ViewModels;

public class OnboardingListViewModel
{
    public List<EmployeeOnboarding> Onboardings { get; set; } = [];

    public List<EmployeeOnboardingTask> OpenTasks { get; set; } = [];

    public List<OnboardingTemplate> Templates { get; set; } = [];

    public int ActiveCount { get; set; }

    public int CompletedCount { get; set; }

    public int OverdueTaskCount { get; set; }
}

public class OnboardingDetailViewModel
{
    public EmployeeOnboarding Onboarding { get; set; } = null!;
}

public class OnboardingStartFormViewModel
{
    public int EmployeeId { get; set; }

    public int OnboardingTemplateId { get; set; }

    public List<Employee> Employees { get; set; } = [];

    public List<OnboardingTemplate> Templates { get; set; } = [];

    public IEnumerable<SelectListItem> EmployeeOptions => Employees
        .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = $"{e.FullName} ({e.EmployeeCode})" });

    public IEnumerable<SelectListItem> TemplateOptions => Templates
        .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name });
}

public class OnboardingTemplateFormViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public List<OnboardingTaskTemplateFormViewModel> Tasks { get; set; } = [];
}

public class OnboardingTaskTemplateFormViewModel
{
    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [Range(1, 365)]
    public int DueDayOffset { get; set; } = 7;
}