using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ResumeAI.Export.API.Rendering;

public interface IPdfRenderer
{
    byte[] Render(ResumeData data);
}

public class PdfRenderer : IPdfRenderer
{
    public PdfRenderer()
    {
        // Set QuestPDF license to Community (free)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(ResumeData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Column(col =>
                {
                    // Header — Name and contact
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(data.FullName)
                                .FontSize(22).Bold();

                            c.Item().Text(data.TargetJobTitle ?? string.Empty)
                                .FontSize(12).FontColor(Colors.Grey.Medium);

                            c.Item().Text(data.Email)
                                .FontSize(10);
                        });
                    });

                    col.Item().PaddingVertical(10)
                        .LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // Sections
                    foreach (var section in data.Sections)
                    {
                        col.Item().PaddingTop(8).Column(s =>
                        {
                            s.Item().Text(section.Title)
                                .FontSize(12).Bold()
                                .FontColor(Colors.Blue.Darken2);

                            s.Item().PaddingTop(2)
                                .LineHorizontal(0.5f)
                                .LineColor(Colors.Grey.Lighten2);

                            s.Item().PaddingTop(4)
                                .Text(section.Content)
                                .FontSize(10);
                        });
                    }
                });
            });
        });

        return document.GeneratePdf();
    }
}

// Data models for rendering
public class ResumeData
{
    public string FullName { get; set; } = string.Empty;
    public string? TargetJobTitle { get; set; }
    public string Email { get; set; } = string.Empty;
    public IList<SectionData> Sections { get; set; } = new List<SectionData>();
}

public class SectionData
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}