using AIKnowledgeBase.Core.DTOs;

namespace AIKnowledgeBase.Core.Interfaces;

public interface ISearchEngine
{
    Task BuildIndexAsync();
    Task<List<SourceDto>> SearchAsync(string query, int? categoryId = null, int topK = 5);
    Task AddDocumentAsync(int documentId, List<string> chunks);
    Task RemoveDocumentAsync(int documentId);
}
