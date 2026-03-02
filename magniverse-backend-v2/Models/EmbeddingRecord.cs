namespace MagniverseBackend.Models
{
    public class EmbeddingRecord
    {
        public string DocumentName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<float> Vector { get; set; } = new();
    }
}