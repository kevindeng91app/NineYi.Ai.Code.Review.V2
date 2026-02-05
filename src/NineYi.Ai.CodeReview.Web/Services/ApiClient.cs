using System.Net.Http.Json;
using NineYi.Ai.CodeReview.Web.Models;

namespace NineYi.Ai.CodeReview.Web.Services;

public interface IApiClient
{
    // Platform Settings
    Task<List<PlatformSettingsDto>> GetPlatformSettingsAsync();
    Task<PlatformSettingsDto?> GetPlatformSettingsByPlatformAsync(int platform);
    Task<PlatformSettingsDto> UpsertPlatformSettingsAsync(int platform, UpsertPlatformSettingsRequest request);

    // Repositories
    Task<List<RepositoryDto>> GetRepositoriesAsync();
    Task<RepositoryDto?> GetRepositoryAsync(Guid id);
    Task<RepositoryDto> CreateRepositoryAsync(CreateRepositoryRequest request);
    Task<RepositoryDto> UpdateRepositoryAsync(Guid id, UpdateRepositoryRequest request);
    Task DeleteRepositoryAsync(Guid id);
    Task<RepositoryLookupResult?> LookupRepositoryAsync(int platform, string fullName);

    // Rules
    Task<List<RuleDto>> GetRulesAsync();
    Task<RuleDto?> GetRuleAsync(Guid id);
    Task<RuleDto> CreateRuleAsync(CreateRuleRequest request);
    Task<RuleDto> UpdateRuleAsync(Guid id, UpdateRuleRequest request);
    Task DeleteRuleAsync(Guid id);
    Task<List<RuleDto>> GetRulesByRepositoryAsync(Guid repositoryId);
    Task MapRuleToRepositoryAsync(Guid ruleId, Guid repositoryId, RuleMappingRequest? request = null);
    Task UnmapRuleFromRepositoryAsync(Guid ruleId, Guid repositoryId);

    // HotKeywords
    Task<List<HotKeywordDto>> GetHotKeywordsAsync();
    Task<HotKeywordDto?> GetHotKeywordAsync(Guid id);
    Task<HotKeywordDto> CreateHotKeywordAsync(CreateHotKeywordRequest request);
    Task<HotKeywordDto> UpdateHotKeywordAsync(Guid id, UpdateHotKeywordRequest request);
    Task DeleteHotKeywordAsync(Guid id);

    // Statistics
    Task<UsageSummaryDto> GetUsageSummaryAsync(DateOnly? fromDate = null, DateOnly? toDate = null);
    Task<List<RuleStatisticsDto>> GetTopTriggeredRulesAsync(int top = 10);
}

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Platform Settings
    public async Task<List<PlatformSettingsDto>> GetPlatformSettingsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<PlatformSettingsDto>>("api/platform-settings") ?? new();
    }

    public async Task<PlatformSettingsDto?> GetPlatformSettingsByPlatformAsync(int platform)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<PlatformSettingsDto>($"api/platform-settings/{platform}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<PlatformSettingsDto> UpsertPlatformSettingsAsync(int platform, UpsertPlatformSettingsRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/platform-settings/{platform}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PlatformSettingsDto>() ?? throw new Exception("Failed to save platform settings");
    }

    // Repositories
    public async Task<List<RepositoryDto>> GetRepositoriesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<RepositoryDto>>("api/repositories") ?? new();
    }

    public async Task<RepositoryDto?> GetRepositoryAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<RepositoryDto>($"api/repositories/{id}");
    }

    public async Task<RepositoryDto> CreateRepositoryAsync(CreateRepositoryRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/repositories", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RepositoryDto>() ?? throw new Exception("Failed to create repository");
    }

    public async Task<RepositoryDto> UpdateRepositoryAsync(Guid id, UpdateRepositoryRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/repositories/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RepositoryDto>() ?? throw new Exception("Failed to update repository");
    }

    public async Task DeleteRepositoryAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/repositories/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<RepositoryLookupResult?> LookupRepositoryAsync(int platform, string fullName)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<RepositoryLookupResult>(
                $"api/repositories/lookup?platform={platform}&fullName={Uri.EscapeDataString(fullName)}");
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    // Rules
    public async Task<List<RuleDto>> GetRulesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<RuleDto>>("api/rules") ?? new();
    }

    public async Task<RuleDto?> GetRuleAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<RuleDto>($"api/rules/{id}");
    }

    public async Task<RuleDto> CreateRuleAsync(CreateRuleRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/rules", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RuleDto>() ?? throw new Exception("Failed to create rule");
    }

    public async Task<RuleDto> UpdateRuleAsync(Guid id, UpdateRuleRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/rules/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RuleDto>() ?? throw new Exception("Failed to update rule");
    }

    public async Task DeleteRuleAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/rules/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<RuleDto>> GetRulesByRepositoryAsync(Guid repositoryId)
    {
        return await _httpClient.GetFromJsonAsync<List<RuleDto>>($"api/rules/repository/{repositoryId}") ?? new();
    }

    public async Task MapRuleToRepositoryAsync(Guid ruleId, Guid repositoryId, RuleMappingRequest? request = null)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/rules/{ruleId}/repositories/{repositoryId}", request ?? new RuleMappingRequest());
        response.EnsureSuccessStatusCode();
    }

    public async Task UnmapRuleFromRepositoryAsync(Guid ruleId, Guid repositoryId)
    {
        var response = await _httpClient.DeleteAsync($"api/rules/{ruleId}/repositories/{repositoryId}");
        response.EnsureSuccessStatusCode();
    }

    // HotKeywords
    public async Task<List<HotKeywordDto>> GetHotKeywordsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<HotKeywordDto>>("api/hotkeywords") ?? new();
    }

    public async Task<HotKeywordDto?> GetHotKeywordAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<HotKeywordDto>($"api/hotkeywords/{id}");
    }

    public async Task<HotKeywordDto> CreateHotKeywordAsync(CreateHotKeywordRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/hotkeywords", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HotKeywordDto>() ?? throw new Exception("Failed to create hot keyword");
    }

    public async Task<HotKeywordDto> UpdateHotKeywordAsync(Guid id, UpdateHotKeywordRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/hotkeywords/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HotKeywordDto>() ?? throw new Exception("Failed to update hot keyword");
    }

    public async Task DeleteHotKeywordAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/hotkeywords/{id}");
        response.EnsureSuccessStatusCode();
    }

    // Statistics
    public async Task<UsageSummaryDto> GetUsageSummaryAsync(DateOnly? fromDate = null, DateOnly? toDate = null)
    {
        var from = fromDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var to = toDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return await _httpClient.GetFromJsonAsync<UsageSummaryDto>($"api/statistics/usage?fromDate={from:yyyy-MM-dd}&toDate={to:yyyy-MM-dd}")
            ?? new UsageSummaryDto();
    }

    public async Task<List<RuleStatisticsDto>> GetTopTriggeredRulesAsync(int top = 10)
    {
        return await _httpClient.GetFromJsonAsync<List<RuleStatisticsDto>>($"api/statistics/top-triggered-rules?top={top}") ?? new();
    }
}
