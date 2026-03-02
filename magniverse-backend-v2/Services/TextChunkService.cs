public class TextChunkService
{
    public List<string> SplitIntoChunks(List<string> pages)
    {
        var chunks = new List<string>();

        foreach (var page in pages)
        {
            int chunkSize = 500;   // 500 characters per chunk

            for (int i = 0; i < page.Length; i += chunkSize)
            {
                int length = Math.Min(chunkSize, page.Length - i);
                string chunk = page.Substring(i, length);

                chunks.Add(chunk);
            }
        }

        return chunks;
    }
}