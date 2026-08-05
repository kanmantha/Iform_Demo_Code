using IForm.Web.Models;
using IForm.Web.Services;

namespace IForm.Tests;

public class SiteQueryPdfServiceTests
{
    [Fact]
    public void BuildReport_ReturnsValidPdfBytes()
    {
        var user = TestData.User("u1", "John Carter", "john@iform.app");
        var query = new SiteQuery
        {
            Id = 1,
            QueryNumber = "QY-001",
            IpoNumber = "556",
            Project = "Hallmark",
            Category = QueryCategory.Missing,
            Status = QueryStatus.InProgress,
            Description = "Approved BOQ mullion profiles are missing on site.",
            QuantityNos = 12,
            QuantitySqm = 4.5m,
            ProductCode = "AL-2250",
            RaisedById = user.Id,
            RaisedBy = user,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            SlabTargetDate = DateTime.UtcNow.AddDays(5),
            Comments =
            {
                new QueryComment
                {
                    Body = "Confirmed missing, production is fabricating replacements.",
                    User = user,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            }
        };

        var webRoot = Path.Combine(Path.GetTempPath(), $"iform-pdf-{Guid.NewGuid():N}", "wwwroot");

        var bytes = SiteQueryPdfService.BuildReport(query, webRoot);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1000, "PDF should be a meaningful size");
        var header = System.Text.Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void BuildReport_HandlesEmptyQuery_WithoutPhotos()
    {
        var query = new SiteQuery
        {
            Id = 2,
            QueryNumber = "QY-002",
            IpoNumber = "571",
            Project = "SRR Khammam",
            Category = QueryCategory.DesignMistake,
            Status = QueryStatus.Pending,
            Description = "Section clashes with shop drawing.",
            RaisedById = "u1",
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        var bytes = SiteQueryPdfService.BuildReport(query, Path.GetTempPath());

        var header = System.Text.Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }
}
