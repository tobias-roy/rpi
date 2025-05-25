using System.Text;
using System.Text.Json;

namespace Web.Services;

public interface IBaseService {
    Task<T?> GetAsync<T>(string uri, CancellationToken cancellationToken = default);
    Task<T?> PostAsync<T>(string uri, T data, CancellationToken cancellationToken = default);
}

public class BaseService : IBaseService
{
        private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    protected BaseService(HttpClient httpClient){
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task<T?> GetAsync<T>(string uri, CancellationToken cancellationToken)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(uri, cancellationToken);
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(content))
                return default;

            return JsonSerializer.Deserialize<T>(content, _jsonOptions)
                ?? throw new InvalidOperationException("Deserialization returned null.");
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Error during GET request to {uri}", ex);
        }
    }

    public async Task<T?> PostAsync<T>(string uri, T? data, CancellationToken cancellationToken)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data), "Data cannot be null for POST requests.");

        try
        {
            string json = JsonSerializer.Serialize(data);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(uri, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(responseContent))
                return default;

            return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions)
                ?? throw new InvalidOperationException("Deserialization returned null.");
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Error during POST request to {uri}", ex);
        }
    }
}