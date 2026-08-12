using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OrmDataAccess.Backend;
using OrmDataAccess.Shared;

#if !NETFRAMEWORK
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
#endif

var cfg = DataAccessConfiguration.Load();
var frontend = Environment.GetEnvironmentVariable("FRONTEND_DOTNET") ?? Detect("FrontendDotnetVersion") ?? "6";
var backend = Environment.GetEnvironmentVariable("BACKEND_DOTNET") ?? Detect("BackendDotnetVersion") ?? "8";
var branch = Environment.GetEnvironmentVariable("BRANCH_NAME") ?? Detect("BranchName") ?? "CSharp_FE6_BE8";
var version = VersionInfo.FromEnvironment(frontend, backend, branch);
var orm = new OrmComposition(cfg, version);
orm.SeedIfRequested();

#if NETFRAMEWORK
await FrameworkHost.RunAsync(cfg.ApiBaseUrl, orm.Service, version);
#else
await ModernHost.RunAsync(cfg, orm.Service, version);
#endif

static string? Detect(string prop)
{
    var props = Path.Combine(Directory.GetCurrentDirectory(), "Directory.Build.props");
    if (!File.Exists(props))
        props = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Directory.Build.props"));
    if (!File.Exists(props)) return null;
    var text = File.ReadAllText(props);
    var m = Regex.Match(text, "<" + prop + ">(.*?)</" + prop + ">");
    return m.Success ? m.Groups[1].Value.Trim() : null;
}

#if !NETFRAMEWORK
static class ModernHost
{
    public static async Task RunAsync(DataAccessConfiguration cfg, DataAccessService service, VersionInfo version)
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.WebHost.UseUrls(cfg.ApiBaseUrl);
        builder.Services.AddSingleton(service);
        builder.Services.AddSingleton(version);
        builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("orm-data-access-backend"))
            .WithTracing(t => t
                .AddAspNetCoreInstrumentation()
                .AddSource("OrmDataAccess.Backend")
                .AddConsoleExporter());
        var app = builder.Build();
        app.UseCors();
        DashboardUi.Map(app, service, version);
        DataAccessApi.Map(app, service, version);
        Console.WriteLine("ORM Data Access API listening on " + cfg.ApiBaseUrl);
        Console.WriteLine("ORM Dashboard UI: " + cfg.ApiBaseUrl.TrimEnd('/') + "/");
        await app.RunAsync();
    }
}
#endif

#if NETFRAMEWORK
static class FrameworkHost
{
    public static async Task RunAsync(string url, DataAccessService service, VersionInfo version)
    {
        if (!url.EndsWith("/")) url += "/";
        var listener = new HttpListener();
        listener.Prefixes.Add(url);
        listener.Start();
        Console.WriteLine("ORM Data Access API (net48 HttpListener) listening on " + url);
        Console.WriteLine("ORM Dashboard UI: " + url);
        while (true)
        {
            var ctx = await listener.GetContextAsync();
            _ = Task.Run(() => Handle(ctx, service, version));
        }
    }

    static void Handle(HttpListenerContext ctx, DataAccessService service, VersionInfo version)
    {
        try
        {
            if (DashboardUi.TryHandleFramework(ctx, service, version))
                return;

            var path = ctx.Request.Url?.AbsolutePath ?? "/";
            var method = ctx.Request.HttpMethod?.ToUpperInvariant() ?? "GET";
            object payload;
            if (path.Equals("/health", StringComparison.OrdinalIgnoreCase))
                payload = service.Health();
            else if (path.Equals("/version", StringComparison.OrdinalIgnoreCase))
                payload = new
                {
                    application = version.Application,
                    frontend_dotnet = version.FrontendDotnet,
                    backend_dotnet = version.BackendDotnet,
                    branch = version.Branch,
                    orm = version.Orm
                };
            else if (path.Equals("/orm/stats", StringComparison.OrdinalIgnoreCase))
                payload = service.Stats();
            else if (path.StartsWith("/orm/", StringComparison.OrdinalIgnoreCase))
            {
                var key = Uri.UnescapeDataString(path.Substring("/orm/".Length));
                if (method == "GET")
                {
                    var prefer = string.Equals(ctx.Request.QueryString["preferReplica"], "true", StringComparison.OrdinalIgnoreCase);
                    payload = service.Get(key, prefer);
                }
                else if (method == "PUT")
                {
                    using var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding);
                    var body = reader.ReadToEnd();
                    payload = DataAccessApi.ParsePut(service, key, body);
                }
                else if (method == "DELETE")
                    payload = service.Delete(key);
                else
                    payload = new { status = "error", message = "method_not_allowed" };
            }
            else
                payload = new { status = "error", message = "not_found" };

            var json = DataAccessApi.Json(payload);
            var bytes = Encoding.UTF8.GetBytes(json);
            ctx.Response.ContentType = "application/json";
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.StatusCode = 200;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
        }
        catch (Exception ex)
        {
            var bytes = Encoding.UTF8.GetBytes("{\"status\":\"error\",\"message\":\"" + ex.Message.Replace("\"", "'") + "\"}");
            ctx.Response.StatusCode = 500;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
        }
        finally
        {
            ctx.Response.OutputStream.Close();
        }
    }
}
#endif

public partial class Program { }
