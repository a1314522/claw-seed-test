using System.Text.RegularExpressions;
using AIKnowledgeBase.Core.DTOs;
using AIKnowledgeBase.Core.Interfaces;
using AIKnowledgeBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIKnowledgeBase.Infrastructure.Services;

public class SearchEngineService : ISearchEngine
{
    private readonly AppDbContext _context;
    private readonly Dictionary<int, List<(string text, int docId, int catId)>> _index = new();
    private readonly HashSet<string> _stopWords = new() { "的", "是", "在", "和", "了", "有", "我", "他", "她", "它", "们", "这", "那", "为", "与", "或", "及", "等", "the", "a", "an", "is", "are", "was", "were", "be", "been", "being", "have", "has", "had", "do", "does", "did", "will", "would", "could", "should", "may", "might", "must", "can", "shall" };

    public SearchEngineService(AppDbContext context)
    {
        _context = context;
    }

    public async Task BuildIndexAsync()
    {
        _index.Clear();
        var chunks = await _context.DocumentChunks
            .Include(c => c.Document)
            .ToListAsync();

        foreach (var chunk in chunks)
        {
            if (!_index.ContainsKey(chunk.DocumentId))
                _index[chunk.DocumentId] = new List<(string, int, int)>();
            _index[chunk.DocumentId].Add((chunk.Text, chunk.DocumentId, chunk.Document.CategoryId));
        }
    }

    public async Task<List<SourceDto>> SearchAsync(string query, int? categoryId = null, int topK = 5)
    {
        if (!_index.Any())
            await BuildIndexAsync();

        var queryTokens = Tokenize(query);
        var scores = new Dictionary<int, double>();
        var allDocs = _index.Values.SelectMany(v => v).ToList();

        foreach (var doc in allDocs)
        {
            if (categoryId.HasValue && doc.catId != categoryId.Value)
                continue;

            var docTokens = Tokenize(doc.text);
            var tfidf = ComputeTfIdf(queryTokens, docTokens, allDocs);
            var jaccard = ComputeJaccard(queryTokens, docTokens);
            var score = tfidf * 0.7 + jaccard * 10.0;

            if (score > 0)
            {
                if (!scores.ContainsKey(doc.docId) || scores[doc.docId] < score)
                    scores[doc.docId] = score;
            }
        }

        return scores.OrderByDescending(kv => kv.Value)
            .Take(topK)
            .Select(kv => new SourceDto
            {
                Source = _context.Documents.Find(kv.Key)?.OriginalName ?? $"doc_{kv.Key}",
                Similarity = Math.Round(kv.Value, 4),
                DocumentId = kv.Key
            })
            .ToList();
    }

    public Task AddDocumentAsync(int documentId, List<string> chunks)
    {
        if (!_index.ContainsKey(documentId))
            _index[documentId] = new List<(string, int, int)>();
        return Task.CompletedTask;
    }

    public Task RemoveDocumentAsync(int documentId)
    {
        _index.Remove(documentId);
        return Task.CompletedTask;
    }

    private List<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        // English words
        var englishMatches = Regex.Matches(text.ToLowerInvariant(), @"[a-z0-9]+");
        tokens.AddRange(englishMatches.Select(m => m.Value).Where(t => t.Length > 2 && !_stopWords.Contains(t)));
        // Chinese characters and 2-3 char phrases
        var chineseChars = Regex.Matches(text, @"[\u4e00-\u9fff]").Select(m => m.Value).ToList();
        tokens.AddRange(chineseChars);
        for (int i = 0; i < chineseChars.Count - 1; i++)
            tokens.Add(chineseChars[i] + chineseChars[i + 1]);
        for (int i = 0; i < chineseChars.Count - 2; i++)
            tokens.Add(chineseChars[i] + chineseChars[i + 1] + chineseChars[i + 2]);
        return tokens;
    }

    private double ComputeTfIdf(List<string> queryTokens, List<string> docTokens, List<(string text, int docId, int catId)> allDocs)
    {
        double score = 0;
        var docFreq = new Dictionary<string, int>();
        foreach (var token in queryTokens.Distinct())
        {
            int df = allDocs.Count(d => Tokenize(d.text).Contains(token));
            docFreq[token] = df;
        }
        foreach (var token in queryTokens)
        {
            int tf = docTokens.Count(t => t == token);
            int df = docFreq.GetValueOrDefault(token, 1);
            double idf = Math.Log((allDocs.Count + 1.0) / (df + 1.0)) + 1.0;
            score += tf * idf;
        }
        return score;
    }

    private double ComputeJaccard(List<string> queryTokens, List<string> docTokens)
    {
        var qSet = queryTokens.ToHashSet();
        var dSet = docTokens.ToHashSet();
        var intersection = qSet.Intersect(dSet).Count();
        var union = qSet.Union(dSet).Count();
        return union == 0 ? 0 : (double)intersection / union;
    }
}
