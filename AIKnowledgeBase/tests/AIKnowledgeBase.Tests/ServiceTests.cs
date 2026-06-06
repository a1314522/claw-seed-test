using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using AIKnowledgeBase.Core.DTOs;
using AIKnowledgeBase.Core.Entities;
using AIKnowledgeBase.Infrastructure.Data;
using AIKnowledgeBase.Infrastructure.Identity;
using AIKnowledgeBase.Infrastructure.Services;
using Xunit;

namespace AIKnowledgeBase.Tests;

public class SearchEngineTests
{
    private readonly AppDbContext _context;
    private readonly SearchEngineService _engine;

    public SearchEngineTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("SearchTestDb")
            .Options;
        _context = new AppDbContext(options);
        _engine = new SearchEngineService(_context);

        // Seed data
        var doc = new Document { Id = 1, FileName = "test.txt", OriginalName = "test.txt", CategoryId = 1 };
        _context.Documents.Add(doc);
        _context.DocumentChunks.Add(new DocumentChunk
        {
            DocumentId = 1,
            ChunkIndex = 0,
            Text = "这是一个关于人工智能的测试文档。人工智能在现代社会中扮演着越来越重要的角色。"
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task Search_ReturnsRelevantResults()
    {
        await _engine.BuildIndexAsync();
        var results = await _engine.SearchAsync("人工智能", 1, 5);

        Assert.NotNull(results);
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task Search_WithNoMatch_ReturnsEmpty()
    {
        await _engine.BuildIndexAsync();
        var results = await _engine.SearchAsync("完全不存在的词", 1, 5);

        Assert.NotNull(results);
        Assert.Empty(results);
    }
}

public class DocumentParserTests
{
    private readonly DocumentParserService _parser = new();

    [Fact]
    public void CanParse_Txt_ReturnsTrue()
    {
        Assert.True(_parser.CanParse("test.txt"));
    }

    [Fact]
    public void CanParse_Unsupported_ReturnsFalse()
    {
        Assert.False(_parser.CanParse("test.exe"));
    }

    [Fact]
    public async Task ExtractText_Txt_ReturnsContent()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Hello World"));
        var result = await _parser.ExtractTextAsync(stream, "test.txt");
        Assert.Contains("Hello World", result);
    }
}

public class JwtServiceTests
{
    [Fact]
    public void GenerateToken_ReturnsValidToken()
    {
        var service = new JwtService("ThisIsAVeryLongSecretKeyForTesting123!", 60);
        var user = new UserInfo { Id = 1, Username = "test", IsAdmin = true, Permissions = new List<string> { "DocumentView" } };
        var token = service.GenerateToken(user);

        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }
}

public class PasswordHasherTests
{
    [Fact]
    public void HashPassword_ReturnsHash()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.HashPassword("test123");

        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.True(hasher.VerifyPassword("test123", hash));
        Assert.False(hasher.VerifyPassword("wrong", hash));
    }
}
