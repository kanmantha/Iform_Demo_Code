using System.ComponentModel.DataAnnotations;

namespace IForm.Web.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress, Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required, MaxLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string? Department { get; set; }

    [Required, MaxLength(100), Display(Name = "Job title")]
    public string? JobTitle { get; set; }

    [Required, StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare("Password", ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Role")]
    public string Role { get; set; } = "Site Engineer";

    public IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> RoleOptions { get; set; } =
        new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
        {
            new("Site Engineer", "Site Engineer"),
            new("Manager", "Manager")
        };
}

public class UserListViewModel
{
    public AppUserViewModel User { get; set; } = new();

    public string? CurrentRole { get; set; }
}

public class AppUserViewModel
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Department { get; set; }

    public string? JobTitle { get; set; }

    public DateTime CreatedAt { get; set; }
}
