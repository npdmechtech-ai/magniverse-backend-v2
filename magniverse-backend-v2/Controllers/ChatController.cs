using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly EmbeddingService _embeddingService;

    public ChatController(EmbeddingService embeddingService)
    {
        _embeddingService = embeddingService;
    }

    [HttpGet("ask")]
    public async Task<IActionResult> Ask(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return BadRequest("Question is required.");

        var answer = await _embeddingService.GenerateAnswer(question);

        return Ok(new { Answer = answer });
    }
}