using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using OrmDataAccess.Shared;

namespace OrmDataAccess.Backend
{
    public static class DataAccessApi
    {
#if !NETFRAMEWORK
        public static void Map(Microsoft.AspNetCore.Builder.WebApplication app, DataAccessService service, VersionInfo version)
        {
            app.MapGet("/health", () => Microsoft.AspNetCore.Http.Results.Json(service.Health()));
            app.MapGet("/version", () => Microsoft.AspNetCore.Http.Results.Json(new
            {
                application = version.Application,
                frontend_dotnet = version.FrontendDotnet,
                backend_dotnet = version.BackendDotnet,
                branch = version.Branch,
                orm = version.Orm
            }));
            app.MapGet("/orm/stats", () => Microsoft.AspNetCore.Http.Results.Json(service.Stats()));
            app.MapGet("/orm/{key}", (string key, bool? preferReplica) =>
                Microsoft.AspNetCore.Http.Results.Json(service.Get(Uri.UnescapeDataString(key), preferReplica == true)));
            app.MapPut("/orm/{key}", async (string key, Microsoft.AspNetCore.Http.HttpRequest http) =>
            {
                using var reader = new StreamReader(http.Body);
                var body = await reader.ReadToEndAsync();
                return Microsoft.AspNetCore.Http.Results.Json(ParsePut(service, key, body));
            });
            app.MapDelete("/orm/{key}", (string key) =>
                Microsoft.AspNetCore.Http.Results.Json(service.Delete(Uri.UnescapeDataString(key))));
        }
#endif

        public static DataAccessResponse ParsePut(DataAccessService service, string key, string body)
        {
            var value = body ?? string.Empty;
            var kind = "entity";
            if (!string.IsNullOrWhiteSpace(body) && body.TrimStart().StartsWith("{", StringComparison.Ordinal))
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("value", out var v)) value = v.GetString() ?? string.Empty;
                    if (doc.RootElement.TryGetProperty("kind", out var k)) kind = k.GetString() ?? "entity";
                }
                catch { /* raw body */ }
            }
            return service.Put(new DataAccessRequest
            {
                Key = Uri.UnescapeDataString(key),
                Value = value,
                Kind = kind
            });
        }

        public static string Json(object payload) =>
            JsonSerializer.Serialize(payload);
    }
}
