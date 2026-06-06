using System.Net.Http.Json;
using System.Text.Json;
using AIKnowledgeBase.Core.Interfaces;

namespace AIKnowledgeBase.Infrastructure.Services;

public class OllamaLLMService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _model;

    public bool IsAvailable { get; private set; }

    public OllamaLLMService(HttpClient httpClient, string baseUrl = "http://localhost:11434", string model = "qwen2.5")
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl;
        _model = model;
        _ = CheckAvailabilityAsync();
    }

    private async Task CheckAvailabilityAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags");
            IsAvailable = response.IsSuccessStatusCode;
        }
        catch { IsAvailable = false; }
    }

    public async Task<string> GenerateAnswerAsync(string question, List<string> contexts)
    {
        if (!IsAvailable)
            return "[错误：Ollama 服务不可用，请检查配置。]";

        var prompt = $"基于以下文档内容回答问题：\n\n{string.Join("\n\n", contexts)}\n\n问题：{question}\n\n请用中文回答：";
        var request = new { model = _model, prompt = prompt, stream = false };

        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/generate", request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("response").GetString() ?? "生成失败";
    }
}
