using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class OrganizationUnit
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    public int? ParentId { get; set; }

    public string? ManagerId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public OrganizationUnit? Parent { get; set; }

    public AppUser? Manager { get; set; }

    public ICollection<OrganizationUnit> Children { get; set; } = new List<OrganizationUnit>();

    public ICollection<AppUser> Members { get; set; } = new List<AppUser>();
}
