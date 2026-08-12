using System;
using System.Net.Http;
using System.Threading.Tasks;
using OrmDataAccess.Frontend;
using OrmDataAccess.Shared;

var api = Environment.GetEnvironmentVariable("ORM_API_URL")
          ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
          ?? "http://localhost:5084/";
if (!api.EndsWith("/")) api += "/";
var ui = Environment.GetEnvironmentVariable("ORM_UI_URL") ?? "http://localhost:5085/";
if (!ui.EndsWith("/")) ui += "/";
var fe = Environment.GetEnvironmentVariable("FRONTEND_DOTNET") ?? "6";
var be = Environment.GetEnvironmentVariable("BACKEND_DOTNET") ?? "8";
var branch = Environment.GetEnvironmentVariable("BRANCH_NAME") ?? "CSharp_FE6_BE8";
var version = VersionInfo.FromEnvironment(fe, be, branch);
var http = new HttpClient { BaseAddress = new Uri(api) };
var client = new DataAccessClient(http);
var dashboard = new DataAccessDashboard(client, version);
if (args.Length > 0 && string.Equals(args[0], "--once", StringComparison.OrdinalIgnoreCase))
{
    await dashboard.RunOnceAsync().ConfigureAwait(false);
    return;
}
Console.WriteLine("Branch=" + branch + " FE=" + fe + " BE=" + be);
Console.WriteLine("API=" + api);
await dashboard.RunUiServerAsync(ui).ConfigureAwait(false);
