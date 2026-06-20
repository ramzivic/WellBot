using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WellBot.Shared.DTOs;

namespace WellBot.Desktop.Services;

public interface IWellBotApiClient
{
    Task<ClientConfigDto?> GetConfigAsync(string language);
    Task PostAnalyticsEventsAsync(List<AnalyticsEventDto> events);

    /// <summary>Whether the last API call succeeded.</summary>
    bool IsConnected { get; }

    /// <summary>Error message from the last failed API call (null when connected).</summary>
    string? LastError { get; }

    /// <summary>Fired when connection status changes.</summary>
    event Action<bool, string?>? ConnectionStatusChanged;
}

public class WellBotApiClient : IWellBotApiClient
{
    private readonly HttpClient _httpClient;

    public bool IsConnected { get; private set; }
    public string? LastError { get; private set; }
    public event Action<bool, string?>? ConnectionStatusChanged;

    public WellBotApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private void SetStatus(bool connected, string? error = null)
    {
        bool changed = IsConnected != connected || LastError != error;
        IsConnected = connected;
        LastError = error;
        if (changed)
            ConnectionStatusChanged?.Invoke(connected, error);
    }

    public async Task<ClientConfigDto?> GetConfigAsync(string language)
    {
        try
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            var result = await _httpClient.GetFromJsonAsync<ClientConfigDto>($"/api/config?language={language}", options);
            SetStatus(true);
            return result;
        }
        catch (HttpRequestException ex)
        {
            SetStatus(false, $"Connexion au serveur impossible: {ex.Message}");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            SetStatus(false, $"Délai d'attente dépassé: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            SetStatus(false, $"Erreur inattendue: {ex.Message}");
            return null;
        }
    }

    public async Task PostAnalyticsEventsAsync(List<AnalyticsEventDto> events)
    {
        try
        {
            await _httpClient.PostAsJsonAsync("/api/analytics/events", events);
            SetStatus(true);
        }
        catch (Exception ex)
        {
            SetStatus(false, $"Erreur d'envoi des analytics: {ex.Message}");
        }
    }
}
