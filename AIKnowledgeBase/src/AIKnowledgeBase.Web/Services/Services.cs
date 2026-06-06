using System.Net.Http.Json;
using System.Net.Http.Headers;
using AIKnowledgeBase.Core.DTOs;

namespace AIKnowledgeBase.Web.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    public UserInfo? CurrentUser { get; private set; }
    public string? Token { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        // Try restore from local storage in real app
    }

    public async Task<ApiResponse<LoginResponse>?> LoginAsync(string username, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", new LoginRequest { Username = username, Password = password });
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        if (result?.Data != null)
        {
            Token = result.Data.AccessToken;
            CurrentUser = result.Data.User;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        }
        return result;
    }

    public async Task<ApiResponse<UserInfo>?> GetCurrentUserAsync()
    {
        if (string.IsNullOrEmpty(Token)) return null;
        var response = await _httpClient.GetAsync("api/auth/me");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ApiResponse<UserInfo>>();
        return null;
    }

    public void Logout()
    {
        Token = null;
        CurrentUser = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public bool HasPermission(string permission) => CurrentUser?.Permissions?.Contains(permission) ?? false;
}

public class CategoryService
{
    private readonly HttpClient _httpClient;
    public CategoryService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<ApiResponse<List<CategoryDto>>?> GetCategoriesAsync()
    {
        var response = await _httpClient.GetAsync("api/categories");
        return await response.Content.ReadFromJsonAsync<ApiResponse<List<CategoryDto>>>();
    }

    public async Task<ApiResponse<CategoryDto>?> GetCategoryAsync(int id)
    {
        var response = await _httpClient.GetAsync($"api/categories/{id}");
        return await response.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
    }

    public async Task<ApiResponse<CategoryDto>?> CreateCategoryAsync(CreateCategoryRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/categories", request);
        return await response.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
    }

    public async Task<ApiResponse<CategoryDto>?> UpdateCategoryAsync(int id, UpdateCategoryRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/categories/{id}", request);
        return await response.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
    }

    public async Task<ApiResponse<object>?> DeleteCategoryAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/categories/{id}");
        return await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
    }
}

public class DocumentService
{
    private readonly HttpClient _httpClient;
    public DocumentService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<ApiResponse<PagedResult<DocumentDto>>?> GetDocumentsAsync(int? categoryId = null, string? search = null, int page = 1, int pageSize = 50)
    {
        var query = new List<string>();
        if (categoryId.HasValue) query.Add($"categoryId={categoryId}");
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        query.Add($"page={page}");
        query.Add($"pageSize={pageSize}");
        var url = "api/documents?" + string.Join("&", query);
        var response = await _httpClient.GetAsync(url);
        return await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<DocumentDto>>>();
    }

    public async Task<ApiResponse<DocumentDto>?> GetDocumentAsync(int id)
    {
        var response = await _httpClient.GetAsync($"api/documents/{id}");
        return await response.Content.ReadFromJsonAsync<ApiResponse<DocumentDto>>();
    }

    public async Task<ApiResponse<DocumentDto>?> UploadDocumentAsync(MultipartFormDataContent content)
    {
        var response = await _httpClient.PostAsync("api/documents/upload", content);
        return await response.Content.ReadFromJsonAsync<ApiResponse<DocumentDto>>();
    }

    public async Task<ApiResponse<DocumentDto>?> UpdateCategoryAsync(int id, int categoryId)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/documents/{id}/category", new UpdateDocumentCategoryRequest { CategoryId = categoryId });
        return await response.Content.ReadFromJsonAsync<ApiResponse<DocumentDto>>();
    }

    public async Task<ApiResponse<object>?> DeleteDocumentAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/documents/{id}");
        return await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
    }
}

public class KnowledgeService
{
    private readonly HttpClient _httpClient;
    public KnowledgeService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<ApiResponse<SearchResultDto>?> SearchAsync(SearchRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/knowledge/search", request);
        return await response.Content.ReadFromJsonAsync<ApiResponse<SearchResultDto>>();
    }
}

public class HistoryService
{
    private readonly HttpClient _httpClient;
    public HistoryService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<ApiResponse<PagedResult<SearchHistoryDto>>?> GetHistoryAsync(int page = 1, int pageSize = 50)
    {
        var response = await _httpClient.GetAsync($"api/history?page={page}&pageSize={pageSize}");
        return await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<SearchHistoryDto>>>();
    }

    public async Task<ApiResponse<object>?> ClearHistoryAsync()
    {
        var response = await _httpClient.DeleteAsync("api/history");
        return await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
    }
}

public class NotificationService
{
    public event Action<string, string>? OnNotify;
    public void Show(string message, string type = "info") => OnNotify?.Invoke(message, type);
}
