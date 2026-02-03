using System.Text.Json;

namespace GameVM.DevTools;

/// <summary>
/// Adapter for HTTP operations
/// </summary>
public interface IHttpService
{
    Task<string> GetStringAsync(string url);
    Task<byte[]> GetByteArrayAsync(string url);
}

/// <summary>
/// Default implementation using HttpClient
/// </summary>
public class DefaultHttpService : IHttpService
{
    private readonly HttpClient _httpClient;

    public DefaultHttpService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<string> GetStringAsync(string url)
    {
        return await _httpClient.GetStringAsync(url);
    }

    public async Task<byte[]> GetByteArrayAsync(string url)
    {
        var response = await _httpClient.GetAsync(url);
        return await response.Content.ReadAsByteArrayAsync();
    }
}
