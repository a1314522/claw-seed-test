using Meilisearch;
using AIKnowledgeBase.Core.Entities;

namespace AIKnowledgeBase.Infrastructure.Services;

public class MeiliSearchService : IMeiliSearchService
{
    private readonly MeilisearchClient _client;
    private readonly string _indexName = "assets";

    public MeiliSearchService(string host, string apiKey)
    {
        _client = new MeilisearchClient(host, apiKey);
    }

    public async Task IndexAssetAsync(Asset asset)
    {
        var index = _client.Index(_indexName);
        var doc = new
        {
            id = asset.Id.ToString(),
            assetCode = asset.AssetCode,
            assetName = asset.AssetName,
            category = asset.Category,
            status = asset.Status,
            location = asset.Location,
            specs = asset.Specs
        };
        await index.AddDocumentsAsync(new[] { doc });
    }

    public async Task UpdateAssetAsync(Asset asset)
    {
        await IndexAssetAsync(asset);
    }

    public async Task DeleteAssetAsync(Guid id)
    {
        var index = _client.Index(_indexName);
        await index.DeleteDocumentAsync(id.ToString());
    }

    public async Task<IEnumerable<Asset>> SearchAsync(string query)
    {
        var index = _client.Index(_indexName);
        var result = await index.SearchAsync<dynamic>(query);
        // Convert results to Asset objects
        return new List<Asset>();
    }
}

public interface IMeiliSearchService
{
    Task IndexAssetAsync(Asset asset);
    Task UpdateAssetAsync(Asset asset);
    Task DeleteAssetAsync(Guid id);
    Task<IEnumerable<Asset>> SearchAsync(string query);
}
