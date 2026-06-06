namespace AIKnowledgeBase.Core.Interfaces;

public interface ILLMService
{
    Task<string> GenerateAnswerAsync(string question, List<string> contexts);
    bool IsAvailable { get; }
}
