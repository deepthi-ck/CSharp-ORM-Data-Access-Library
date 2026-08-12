using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OrmDataAccess.Shared;

namespace OrmDataAccess.Frontend
{
    public sealed class DataAccessClient
    {
        private readonly HttpClient _http;
        public DataAccessClient(HttpClient http) => _http = http ?? throw new ArgumentNullException(nameof(http));
        public string? LastError { get; private set; }
        public EntityEntry? LastEntry { get; private set; }

        public async Task<DataAccessResponse?> PutAsync(string key, string value, string kind = "entity")
        {
            var payload = JsonSerializer.Serialize(new { value, kind });
            var response = await _http.PutAsync("/orm/" + Uri.EscapeDataString(key),
                new StringContent(payload, Encoding.UTF8, "application/json")).ConfigureAwait(false);
            return await ReadAsync(response).ConfigureAwait(false);
        }

        public async Task<DataAccessResponse?> GetAsync(string key, bool preferReplica = false)
        {
            var q = preferReplica ? "?preferReplica=true" : string.Empty;
            var response = await _http.GetAsync("/orm/" + Uri.EscapeDataString(key) + q).ConfigureAwait(false);
            return await ReadAsync(response).ConfigureAwait(false);
        }

        public async Task<DataAccessResponse?> DeleteAsync(string key)
        {
            var response = await _http.DeleteAsync("/orm/" + Uri.EscapeDataString(key)).ConfigureAwait(false);
            return await ReadAsync(response).ConfigureAwait(false);
        }

        public Task<object?> StatsAsync() => _http.GetFromJsonAsync<object>("/orm/stats");
        public Task<object?> HealthAsync() => _http.GetFromJsonAsync<object>("/health");
        public Task<object?> VersionAsync() => _http.GetFromJsonAsync<object>("/version");

        private async Task<DataAccessResponse?> ReadAsync(HttpResponseMessage response)
        {
            try
            {
                var payload = await response.Content.ReadFromJsonAsync<DataAccessResponse>().ConfigureAwait(false);
                if (payload is null) { LastError = "Empty response"; return null; }
                LastError = payload.Success ? null : payload.Message;
                LastEntry = payload.Entry ?? (payload.Found
                    ? new EntityEntry { Key = payload.Key ?? string.Empty, Value = payload.Value ?? string.Empty }
                    : LastEntry);
                return payload;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
        }
    }
}
