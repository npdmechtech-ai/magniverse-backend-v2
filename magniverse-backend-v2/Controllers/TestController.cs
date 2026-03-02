using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly PdfTextExtractor _extractor;
    private readonly TextChunkService _chunkService;
    private readonly IWebHostEnvironment _env;

    public TestController(
        PdfTextExtractor extractor,
        TextChunkService chunkService,
        IWebHostEnvironment env)
    {
        _extractor = extractor;
        _chunkService = chunkService;
        _env = env;
    }

    [HttpGet("readpdf")]
    public IActionResult ReadPdf()
    {
        string filePath = Path.Combine(
            _env.ContentRootPath,
            "PdfStorage",
            "Front Axle Replacement procedure.pdf"
        );

        var pages = _extractor.ExtractText(filePath);

        var chunks = _chunkService.SplitIntoChunks(pages);

        return Ok(chunks);
    }
}