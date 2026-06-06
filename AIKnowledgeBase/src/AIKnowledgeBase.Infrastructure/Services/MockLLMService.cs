using AIKnowledgeBase.Core.Interfaces;

namespace AIKnowledgeBase.Infrastructure.Services;

public class MockLLMService : ILLMService
{
    public bool IsAvailable => true;

    public Task<string> GenerateAnswerAsync(string question, List<string> contexts)
    {
        var context = string.Join("\n", contexts.Take(3));
        var answer = $"[测试模式] 基于检索到的相关内容，回答如下问题：\n\n问题：{question}\n\n检索到的上下文：\n{context}\n\n[注意：当前使用的是测试模式，未接入真实大模型。请配置 Ollama 或其他 LLM 服务以获得真实回答。]";
        return Task.FromResult(answer);
    }
}
