using System.Diagnostics;
using Api.Models;

namespace Web.Services;

public interface IDataService
{
    Task<List<D1Payload>> GetBirdieData(DateTime start, DateTime end, CancellationToken cancellationToken = default);
}

public class DataService(HttpClient httpClient) : BaseService(httpClient), IDataService
{
    public async Task<List<TelemetryData>> GetBirdieData(DateTime start, DateTime end, CancellationToken cancellationToken)
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
}