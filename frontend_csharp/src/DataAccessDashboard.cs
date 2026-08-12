using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using OrmDataAccess.Shared;

namespace OrmDataAccess.Frontend
{
    public sealed class DataAccessDashboard
    {
        private readonly DataAccessClient _client;
        private readonly VersionInfo _version;

        public DataAccessDashboard(DataAccessClient client, VersionInfo version)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _version = version ?? throw new ArgumentNullException(nameof(version));
        }

        public async Task RunOnceAsync()
        {
            Console.WriteLine("C# ORM / Data Access Library");
            Console.WriteLine("Branch: " + _version.Branch);
            Console.WriteLine("Frontend .NET / Framework: " + _version.FrontendDotnet);
            Console.WriteLine("Backend .NET / Framework: " + _version.BackendDotnet);
            var put = await _client.PutAsync("user:1001", "Visvantha", "user").ConfigureAwait(false);
            Console.WriteLine("PUT: " + (put?.Message ?? "fail"));
            var get = await _client.GetAsync("user:1001").ConfigureAwait(false);
            Console.WriteLine("GET: " + (get?.Value ?? "NOT_FOUND"));
            var stats = await _client.StatsAsync().ConfigureAwait(false);
            Console.WriteLine("STATS: " + stats);
        }

        /// <summary>
        /// Hosts a browser UI with HttpListener (BCL only). Forms are handled in C# via DataAccessClient.
        /// </summary>
        public async Task RunUiServerAsync(string uiBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(uiBaseUrl)) throw new ArgumentException("UI URL required", nameof(uiBaseUrl));
            if (!uiBaseUrl.EndsWith("/")) uiBaseUrl += "/";
            var listener = new HttpListener();
            listener.Prefixes.Add(uiBaseUrl);
            listener.Start();
            Console.WriteLine("ORM frontend dashboard UI listening on " + uiBaseUrl);
            Console.WriteLine("Open this URL in your browser. API proxied with C# HttpClient.");
            while (true)
            {
                var ctx = await listener.GetContextAsync().ConfigureAwait(false);
                _ = Task.Run(() => HandleAsync(ctx));
            }
        }

        private async Task HandleAsync(HttpListenerContext ctx)
        {
            try
            {
                var path = ctx.Request.Url?.AbsolutePath ?? "/";
                var method = ctx.Request.HttpMethod?.ToUpperInvariant() ?? "GET";
                if ((path == "/" || path.Equals("/ui", StringComparison.OrdinalIgnoreCase)) && method == "GET")
                {
                    var stats = await _client.StatsAsync().ConfigureAwait(false);
                    await WriteHtmlAsync(ctx, DashboardPage.Render(_version, stats, null, null, "/ui/action")).ConfigureAwait(false);
                    return;
                }

                if (path.Equals("/ui/action", StringComparison.OrdinalIgnoreCase) && method == "POST")
                {
                    using var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding);
                    var body = await reader.ReadToEndAsync().ConfigureAwait(false);
                    var form = DashboardPage.ParseFormUrlEncoded(body);
                    var (html, _) = await ExecuteAsync(form).ConfigureAwait(false);
                    await WriteHtmlAsync(ctx, html).ConfigureAwait(false);
                    return;
                }

                // Convenience proxies so API links in the page work when UI is on :5085
                if (path.Equals("/health", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteJsonAsync(ctx, await _client.HealthAsync().ConfigureAwait(false)).ConfigureAwait(false);
                    return;
                }
                if (path.Equals("/version", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteJsonAsync(ctx, await _client.VersionAsync().ConfigureAwait(false)).ConfigureAwait(false);
                    return;
                }
                if (path.Equals("/orm/stats", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteJsonAsync(ctx, await _client.StatsAsync().ConfigureAwait(false)).ConfigureAwait(false);
                    return;
                }

                ctx.Response.StatusCode = 404;
                var bytes = Encoding.UTF8.GetBytes("not_found");
                ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
                ctx.Response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                try
                {
                    ctx.Response.StatusCode = 500;
                    var bytes = Encoding.UTF8.GetBytes(ex.Message);
                    ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
                    ctx.Response.OutputStream.Close();
                }
                catch { /* ignore */ }
            }
        }

        private async Task<(string html, DataAccessResponse? last)> ExecuteAsync(IReadOnlyDictionary<string, string> form)
        {
            form.TryGetValue("action", out var action);
            form.TryGetValue("key", out var key);
            form.TryGetValue("value", out var value);
            form.TryGetValue("kind", out var kind);
            form.TryGetValue("preferReplica", out var preferRaw);
            action = string.IsNullOrWhiteSpace(action) ? "stats" : action.Trim().ToLowerInvariant();
            key ??= string.Empty;
            value ??= string.Empty;
            kind = string.IsNullOrWhiteSpace(kind) ? "entity" : kind;
            var prefer = string.Equals(preferRaw, "true", StringComparison.OrdinalIgnoreCase);

            DataAccessResponse? last = null;
            string flash;
            switch (action)
            {
                case "put":
                    last = await _client.PutAsync(key, value, kind).ConfigureAwait(false);
                    flash = last?.Success == true ? "PUT = SUCCESS" : ("PUT FAIL: " + (last?.Message ?? _client.LastError ?? "fail"));
                    break;
                case "get":
                    last = await _client.GetAsync(key, prefer).ConfigureAwait(false);
                    flash = last?.Success == true ? ("GET = SUCCESS → " + (last.Value ?? "")) : ("GET: " + (last?.Message ?? "NOT_FOUND"));
                    break;
                case "delete":
                    last = await _client.DeleteAsync(key).ConfigureAwait(false);
                    flash = last?.Success == true ? "DELETE = SUCCESS" : ("DELETE: " + (last?.Message ?? "fail"));
                    break;
                default:
                    flash = "Stats refreshed";
                    break;
            }

            var stats = await _client.StatsAsync().ConfigureAwait(false);
            return (DashboardPage.Render(_version, stats, flash, last, "/ui/action"), last);
        }

        private static Task WriteHtmlAsync(HttpListenerContext ctx, string html)
        {
            var bytes = Encoding.UTF8.GetBytes(html);
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/html; charset=utf-8";
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.OutputStream.Close();
            return Task.CompletedTask;
        }

        private static Task WriteJsonAsync(HttpListenerContext ctx, object? payload)
        {
            var json = payload is null ? "{}" : System.Text.Json.JsonSerializer.Serialize(payload);
            var bytes = Encoding.UTF8.GetBytes(json);
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json; charset=utf-8";
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.OutputStream.Close();
            return Task.CompletedTask;
        }
    }
}
