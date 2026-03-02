using UglyToad.PdfPig;

public class PdfTextExtractor
{
    public List<string> ExtractText(string filePath)
    {
        var pages = new List<string>();

        using (var document = PdfDocument.Open(filePath))
        {
            foreach (var page in document.GetPages())
            {
                pages.Add(page.Text);
            }
        }

        return pages;
    }
}