using System.ComponentModel.DataAnnotations;

namespace IForm.Web.ViewModels;

public class SiteFormViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Address { get; set; }

    [MaxLength(60)]
    public string? City { get; set; }

    [MaxLength(60)]
    public string? Country { get; set; }

    public bool IsActive { get; set; } = true;
}
