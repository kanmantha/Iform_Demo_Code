using Microsoft.AspNetCore.Identity;

namespace IForm.Web.Models;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public string? Department { get; set; }

    public string? JobTitle { get; set; }

    public int? OrganizationUnitId { get; set; }

    public OrganizationUnit? OrganizationUnit { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
