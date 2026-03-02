using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Linq;
using MagniverseBackend.Models;

public class EmbeddingService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public EmbeddingService(IConfiguration config)
    {
        _config = config;
        _httpClient = new HttpClient();
    }

    // 🔵 Create Embedding
    public async Task<List<float>> CreateEmbedding(string text)
    {
        var apiKey = _config["OpenAI:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
            throw new Exception("OpenAI API key not found.");

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = new
        {
            model = "text-embedding-3-small",
            input = text
        };

        var content = new StringContent(
            JsonConvert.SerializeObject(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(
            "https://api.openai.com/v1/embeddings",
            content);

        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception("Embedding API Error: " + responseText);

        dynamic? json = JsonConvert.DeserializeObject(responseText);

        if (json?.data == null)
            throw new Exception("Invalid embedding response.");

        var vector = new List<float>();

        foreach (var v in json.data[0].embedding)
        {
            vector.Add((float)v);
        }

        return vector;
    }

    // 🔵 Cosine Similarity
    private double CosineSimilarity(List<float> vector1, List<float> vector2)
    {
        double dot = 0.0;
        double mag1 = 0.0;
        double mag2 = 0.0;

        for (int i = 0; i < vector1.Count; i++)
        {
            dot += vector1[i] * vector2[i];
            mag1 += Math.Pow(vector1[i], 2);
            mag2 += Math.Pow(vector2[i], 2);
        }

        mag1 = Math.Sqrt(mag1);
        mag2 = Math.Sqrt(mag2);

        return dot / (mag1 * mag2);
    }

    // 🔵 Similarity Search (With Threshold Protection)
    public async Task<List<EmbeddingRecord>> SearchSimilarChunks(string question, int topN = 5)
    {
        double threshold = 0.55; // Adjustable

        var questionVector = await CreateEmbedding(question);
        var records = LoadEmbeddings();

        if (records == null || !records.Any())
            return new List<EmbeddingRecord>();

        var scored = records
            .Select(r => new
            {
                Record = r,
                Score = CosineSimilarity(questionVector, r.Vector)
            })
            .Where(x => x.Score >= threshold)
            .OrderByDescending(x => x.Score)
            .Take(topN)
            .Select(x => x.Record)
            .ToList();

        return scored;
    }

    // 🔵 Generate Final Answer (RAG)
    public async Task<string> GenerateAnswer(string question)
    {
        var relevantChunks = await SearchSimilarChunks(question, 5);

        if (relevantChunks == null || !relevantChunks.Any())
            return "Not available in document.";

        var context = string.Join("\n\n", relevantChunks.Select(r => r.Text));

        var apiKey = _config["OpenAI:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
            return "System configuration error.";

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new {
                    role = "system",
                    content = "You are a document assistant. Answer ONLY using the provided context. If the answer is not found in the context, reply: 'Not available in document.'"
                },
                new {
                    role = "user",
                    content = $"Context:\n{context}\n\nQuestion:\n{question}"
                }
            }
        };

        var content = new StringContent(
            JsonConvert.SerializeObject(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            content);

        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return "System temporarily unavailable.";

        dynamic? json = JsonConvert.DeserializeObject(responseText);

        return json?.choices?[0]?.message?.content?.ToString()
               ?? "Not available in document.";
    }

    // 🔵 Store Embeddings
    public async Task CreateAndStoreEmbeddings(List<string> chunks, string docName)
    {
        var records = new List<EmbeddingRecord>();

        foreach (var chunk in chunks)
        {
            var vector = await CreateEmbedding(chunk);

            records.Add(new EmbeddingRecord
            {
                DocumentName = docName,
                Text = chunk,
                Vector = vector
            });
        }

        var folder = Path.Combine("Embeddings");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var path = Path.Combine(folder, "embeddings.json");

        File.WriteAllText(path, JsonConvert.SerializeObject(records));
    }

    // 🔵 Load Embeddings
    public List<EmbeddingRecord> LoadEmbeddings()
    {
        var path = Path.Combine("Embeddings", "embeddings.json");

        if (!File.Exists(path))
            return new List<EmbeddingRecord>();

        return JsonConvert.DeserializeObject<List<EmbeddingRecord>>(
            File.ReadAllText(path)
        ) ?? new List<EmbeddingRecord>();
    }
}