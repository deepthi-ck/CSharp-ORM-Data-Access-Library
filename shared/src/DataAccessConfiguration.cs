using System;
using System.IO;
using System.Text.Json;

namespace OrmDataAccess.Shared
{
    public sealed class DataAccessConfiguration
    {
        public int EntryTtlSeconds { get; set; } = 300;
        public int MaxEntries { get; set; } = 64;
        public int NodeSlotCount { get; set; } = 3;
        public string ApiBaseUrl { get; set; } = "http://localhost:5084";
        public string SampleDataPath { get; set; } = "data/sample-orm-data.json";

        public static DataAccessConfiguration Load(string? path = null)
        {
            var cfg = new DataAccessConfiguration();
            var candidates = new[]
            {
                path,
                Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "config", "appsettings.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")
            };
            foreach (var p in candidates)
            {
                if (string.IsNullOrWhiteSpace(p) || !File.Exists(p)) continue;
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(p));
                    var root = doc.RootElement;
                    if (root.TryGetProperty("EntryTtlSeconds", out var ttl)) cfg.EntryTtlSeconds = ttl.GetInt32();
                    if (root.TryGetProperty("MaxEntries", out var max)) cfg.MaxEntries = max.GetInt32();
                    if (root.TryGetProperty("NodeSlotCount", out var nodes)) cfg.NodeSlotCount = nodes.GetInt32();
                    if (root.TryGetProperty("ApiBaseUrl", out var url)) cfg.ApiBaseUrl = url.GetString() ?? cfg.ApiBaseUrl;
                    if (root.TryGetProperty("SampleDataPath", out var sample)) cfg.SampleDataPath = sample.GetString() ?? cfg.SampleDataPath;
                    break;
                }
                catch { /* keep defaults */ }
            }
            var envUrl = Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
                         ?? Environment.GetEnvironmentVariable("ORM_API_URL");
            if (!string.IsNullOrWhiteSpace(envUrl)) cfg.ApiBaseUrl = envUrl.Split(';')[0].Trim();
            return cfg;
        }
    }
}
