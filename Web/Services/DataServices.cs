using System.Diagnostics;
using Web.Models;

namespace Web.Services;

public interface IDataService
{
    // Task<List<D1Payload>> GetBirdieData(DateTime? start, DateTime? end, CancellationToken cancellationToken = default);
    Task<List<D1Payload>> GetBirdieData(CancellationToken cancellationToken = default);
}

public class DataService(HttpClient httpClient) : BaseService(httpClient), IDataService
{
    // public async Task<List<D1Payload>> GetBirdieData(DateTime? start, DateTime? end, CancellationToken cancellationToken)
    public async Task<List<D1Payload>> GetBirdieData(CancellationToken cancellationToken)
    {
        try
        {
            // return await GetAsync<List<D1Payload>>($"data?start={Uri.EscapeDataString(start.ToString("O"))}&end={Uri.EscapeDataString(end.ToString("O"))}", cancellationToken) ?? [];
            return await GetAsync<List<D1Payload>>($"data", cancellationToken) ?? [];
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return [];
        }
    }

    public async Task<List<D1Payload>> GetDefaultBirdieData(CancellationToken cancellationToken)
    {
        try
        {
            // return await GetAsync<List<D1Payload>>($"data?start={Uri.EscapeDataString(start.ToString("O"))}&end={Uri.EscapeDataString(end.ToString("O"))}", cancellationToken) ?? [];
            return await GetAsync<List<D1Payload>>($"wemos/historical&=", cancellationToken) ?? [];
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return [];
        }
    }
}