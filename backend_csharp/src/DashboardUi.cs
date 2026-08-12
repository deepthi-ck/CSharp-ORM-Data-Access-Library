using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using OrmDataAccess.Shared;

namespace OrmDataAccess.Backend
{
    /// <summary>
    /// Serves the C# BCL dashboard UI without changing JSON API contracts.
    /// </summary>
    public static class DashboardUi
    {
        public static (string html, int statusCode, string? redirect) HandleGet(DataAccessService service, VersionInfo version)
        {
            var html = DashboardPage.Render(version, service.Stats(), null);
            return (html, 200, null);
        }

        public static (string html, int statusCode, string? redirect) HandleAction(
            DataAccessService service,
            VersionInfo version,
            IReadOnlyDictionary<string, string> form)
        {
            if (service is null) throw new ArgumentNullException(nameof(service));
            if (version is null) throw new ArgumentNullException(nameof(version));
            if (form is null) throw new ArgumentNullException(nameof(form));

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
                    last = service.Put(new DataAccessRequest { Key = key, Value = value, Kind = kind });
                    flash = last.Success ? "PUT = SUCCESS" : ("PUT FAIL: " + last.Message);
                    break;
                case "get":
                    last = service.Get(key, prefer);
                    flash = last.Success ? ("GET = SUCCESS → " + (last.Value ?? "")) : ("GET: " + last.Message);
                    break;
                case "delete":
                    last = service.Delete(key);
                    flash = last.Success ? "DELETE = SUCCESS" : ("DELETE: " + last.Message);
                    break;
                default:
                    flash = "Stats refreshed";
                    break;
            }

            var html = DashboardPage.Render(version, service.Stats(), flash, last);
            return (html, 200, null);
        }

#if !NETFRAMEWORK
        public static void Map(Microsoft.AspNetCore.Builder.WebApplication app, DataAccessService service, VersionInfo version)
        {
            app.MapGet("/", () => Microsoft.AspNetCore.Http.Results.Content(
                DashboardPage.Render(version, service.Stats(), null), "text/html; charset=utf-8"));
            app.MapGet("/ui", () => Microsoft.AspNetCore.Http.Results.Content(
                DashboardPage.Render(version, service.Stats(), null), "text/html; charset=utf-8"));
            app.MapPost("/ui/action", async (Microsoft.AspNetCore.Http.HttpRequest request) =>
            {
                var form = await request.ReadFormAsync().ConfigureAwait(false);
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var key in form.Keys)
                {
                    map[key] = form[key].ToString();
                }
                var result = HandleAction(service, version, map);
                return Microsoft.AspNetCore.Http.Results.Content(result.html, "text/html; charset=utf-8");
            });
        }
#endif

        public static bool TryHandleFramework(HttpListenerContext ctx, DataAccessService service, VersionInfo version)
        {
            var path = ctx.Request.Url?.AbsolutePath ?? "/";
            var method = ctx.Request.HttpMethod?.ToUpperInvariant() ?? "GET";
            if (path.Equals("/", StringComparison.OrdinalIgnoreCase) || path.Equals("/ui", StringComparison.OrdinalIgnoreCase))
            {
                if (method != "GET") return false;
                WriteHtml(ctx, HandleGet(service, version).html);
                return true;
            }
            if (path.Equals("/ui/action", StringComparison.OrdinalIgnoreCase) && method == "POST")
            {
                using var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding);
                var body = reader.ReadToEnd();
                var form = DashboardPage.ParseFormUrlEncoded(body);
                WriteHtml(ctx, HandleAction(service, version, form).html);
                return true;
            }
            return false;
        }

        private static void WriteHtml(HttpListenerContext ctx, string html)
        {
            var bytes = Encoding.UTF8.GetBytes(html);
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/html; charset=utf-8";
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.OutputStream.Close();
        }
    }
}
