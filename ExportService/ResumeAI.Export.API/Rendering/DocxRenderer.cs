using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ResumeAI.Export.API.Rendering;

public interface IDocxRenderer
{
    byte[] Render(ResumeData data);
}

public class DocxRenderer : IDocxRenderer
{
    public byte[] Render(ResumeData data)
    {
        using var stream = new MemoryStream();

        using (var doc = WordprocessingDocument.Create(
            stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // Name heading
            var namePara = body.AppendChild(new Paragraph());
            var nameRun  = namePara.AppendChild(new Run());
            nameRun.AppendChild(new RunProperties(
                new Bold(), new FontSize { Val = "44" }));
            nameRun.AppendChild(new Text(data.FullName));

            // Job title
            if (data.TargetJobTitle is not null)
            {
                var titlePara = body.AppendChild(new Paragraph());
                var titleRun  = titlePara.AppendChild(new Run());
                titleRun.AppendChild(new Text(data.TargetJobTitle));
            }

            // Email
            var emailPara = body.AppendChild(new Paragraph());
            var emailRun  = emailPara.AppendChild(new Run());
            emailRun.AppendChild(new Text(data.Email));

            // Sections
            foreach (var section in data.Sections)
            {
                // Section heading
                var headingPara = body.AppendChild(new Paragraph());
                var headingRun  = headingPara.AppendChild(new Run());
                headingRun.AppendChild(new RunProperties(
                    new Bold(), new FontSize { Val = "28" }));
                headingRun.AppendChild(new Text(section.Title));

                // Section content
                var contentPara = body.AppendChild(new Paragraph());
                var contentRun  = contentPara.AppendChild(new Run());
                contentRun.AppendChild(new Text(section.Content));
            }

            mainPart.Document.Save();
        }

        return stream.ToArray();
    }
}