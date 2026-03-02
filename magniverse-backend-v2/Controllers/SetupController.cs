using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class SetupController : ControllerBase
{
    private readonly PdfTextExtractor _extractor;
    private readonly TextChunkService _chunkService;
    private readonly EmbeddingService _embeddingService;
    private readonly IWebHostEnvironment _env;

    public SetupController(
        PdfTextExtractor extractor,
        TextChunkService chunkService,
        EmbeddingService embeddingService,
        IWebHostEnvironment env)
    {
        _extractor = extractor;
        _chunkService = chunkService;
        _embeddingService = embeddingService;
        _env = env;
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessPdf()
    {
        string fileName = "Front Axle Replacement procedure.pdf";

        string filePath = Path.Combine(
            _env.ContentRootPath,
            "PdfStorage",
            fileName
        );

        var pages = _extractor.ExtractText(filePath);
        var chunks = _chunkService.SplitIntoChunks(pages);

        await _embeddingService.CreateAndStoreEmbeddings(chunks, fileName);

        return Ok("Embeddings created successfully.");
    }
}