using System.ComponentModel.DataAnnotations;
using IForm.Web.Models;

namespace IForm.Web.ViewModels;

public class ProductListViewModel
{
    public string? Search { get; set; }

    public string? Family { get; set; }

    public IReadOnlyList<Product> Products { get; set; } = new List<Product>();

    public IReadOnlyList<string> FamilyOptions { get; set; } = new List<string>();
}

public class ProductFormViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    [Display(Name = "Product code")]
    public string ProductCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Family { get; set; }

    [MaxLength(100)]
    public string? Material { get; set; }

    public string? Specification { get; set; }

    public string? Dimensions { get; set; }

    public string? Project { get; set; }

    public string? Unit { get; set; }

    public bool IsActive { get; set; } = true;
}
