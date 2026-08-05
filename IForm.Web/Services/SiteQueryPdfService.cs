using IForm.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IForm.Web.Services;

public static class SiteQueryPdfService
{
    static SiteQueryPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] BuildReport(SiteQuery query, string webRootPath)
    {
        using var stream = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(style => style.FontSize(10).FontColor(Colors.Grey.Darken3));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("IFORM").FontSize(20).Bold().FontColor("#4f46e5");
                        col.Item().Text("Site Query & Defect Report").FontSize(11).FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"Generated {DateTime.UtcNow.ToLocalTime():dd MMM yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                    row.ConstantItem(180).AlignRight().Column(col =>
                    {
                        col.Item().AlignRight().Text(query.QueryNumber).FontSize(18).Bold().FontColor(Colors.Grey.Darken3);
                        col.Item().AlignRight().Text($"IPO {query.IpoNumber}").FontSize(11).FontColor(Colors.Grey.Darken1);
                    });
                });

                page.Content().PaddingVertical(16).Column(column =>
                {
                    column.Item().Text($"{query.Project}")
                        .FontSize(14).Bold();

                    column.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Element(Chip(query.Status.ToString(), StatusColor(query.Status)));
                        row.RelativeItem().Element(Chip(query.Category.ToString(), Colors.Grey.Darken1));
                        row.RelativeItem().Element(Chip($"{query.DelayDays} day{(query.DelayDays == 1 ? "" : "s")} delay", DelayColor(query.DelayDays)));
                    });

                    column.Spacing(12);

                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12).Column(details =>
                    {
                        details.Item().Text("DISPATCH DETAILS").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                        details.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            Field(table, "Quantity", $"{query.QuantityNos} nos / {query.QuantitySqm} sqm");
                            Field(table, "Verified product", query.ProductCode ?? "Not verified");
                            Field(table, "Raised by", query.RaisedBy?.FullName ?? "Unknown");
                            Field(table, "Raised at", query.CreatedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm"));
                            Field(table, "Slab target date", query.SlabTargetDate?.ToLocalTime().ToString("dd MMM yyyy") ?? "—");
                            Field(table, "Slab completed date", query.SlabCompletedDate?.ToLocalTime().ToString("dd MMM yyyy") ?? "—");
                            Field(table, "Resolved by", query.ResolvedBy?.FullName ?? (query.Status == QueryStatus.Resolved ? "—" : "Not yet resolved"));
                            Field(table, "Resolved at", query.ResolvedAt?.ToLocalTime().ToString("dd MMM yyyy HH:mm") ?? "—");
                        });
                    });

                    column.Item().Text("ISSUE DESCRIPTION").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10)
                        .Text(query.Description).FontSize(10).LineHeight(1.4f);

                    if (query.Photos.Count > 0)
                    {
                        column.Item().Text("PHOTO EVIDENCE").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                        column.Item().Column(photoColumn =>
                        {
                            foreach (var photo in query.Photos)
                            {
                                var bytes = LoadPhoto(photo.FilePath, webRootPath);
                                if (bytes is null)
                                {
                                    continue;
                                }

                                photoColumn.Item()
                                    .Width(180).Height(135)
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Image(bytes)
                                    .FitWidth()
                                    .FitHeight();
                            }
                        });
                    }

                    if (query.Comments.Count > 0)
                    {
                        column.Item().Text("COMMENTS & UPDATES").FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Column(commentColumn =>
                        {
                            foreach (var comment in query.Comments.OrderBy(c => c.CreatedAt))
                            {
                                commentColumn.Item().Padding(8).Column(item =>
                                {
                                    item.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text(comment.User?.FullName ?? "System").SemiBold().FontSize(9);
                                        row.ConstantItem(90).AlignRight().Text(comment.CreatedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm")).FontSize(8).FontColor(Colors.Grey.Medium);
                                    });
                                    item.Item().PaddingTop(2).Text(comment.Body).FontSize(9).LineHeight(1.35f);
                                });
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Grey.Medium));
                    text.Span("IFORM Safety & Incident Management  ·  ");
                    text.Span(query.QueryNumber).Bold();
                    text.Span("  ·  Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf(stream);

        return stream.ToArray();
    }

    private static byte[]? LoadPhoto(string? path, string webRootPath)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var relative = path.TrimStart('/');
        var fullPath = Path.Combine(webRootPath, relative);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        try
        {
            return File.ReadAllBytes(fullPath);
        }
        catch
        {
            return null;
        }
    }

    private static void Field(TableDescriptor table, string label, string value)
    {
        table.Cell().Padding(4).Text(label).FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
        table.Cell().Padding(4).Text(value).FontSize(9);
    }

    private static Func<IContainer, IContainer> Chip(string text, string color)
        => container =>
        {
            var box = container
                .PaddingRight(6)
                .MaxWidth(220)
                .Background(color)
                .PaddingHorizontal(4f)
                .PaddingVertical(1f)
                .AlignCenter();
            box.Text(t => t
                .Span(text)
                .FontSize(9)
                .Bold()
                .FontColor(Colors.White));
            return box;
        };

    private static string StatusColor(QueryStatus status) => status switch
    {
        QueryStatus.Pending => "#f59e0b",
        QueryStatus.InProgress => "#6366f1",
        QueryStatus.Resolved => "#10b981",
        _ => Colors.Grey.Darken1
    };

    private static string DelayColor(int days) => days switch
    {
        <= 0 => "#10b981",
        <= 7 => "#f59e0b",
        <= 30 => "#f97316",
        _ => "#ef4444"
    };
}
