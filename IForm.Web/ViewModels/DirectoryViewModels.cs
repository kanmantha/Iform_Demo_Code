using IForm.Web.Models;

namespace IForm.Web.ViewModels;

public class DirectoryListViewModel
{
    public List<DirectoryEntry> Entries { get; set; } = [];

    public string? Search { get; set; }

    public int TotalCount { get; set; }
}
