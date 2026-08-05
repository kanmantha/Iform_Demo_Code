using System.ComponentModel.DataAnnotations;
using IForm.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IForm.Web.ViewModels;

public class OrganizationUnitListViewModel
{
    public IReadOnlyList<OrganizationUnit> Units { get; set; } = new List<OrganizationUnit>();

    public int MemberCount { get; set; }

    public int ActiveCount { get; set; }
}

public class OrganizationUnitFormViewModel
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

    public IEnumerable<SelectListItem> Parents { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> Managers { get; set; } = new List<SelectListItem>();
}
