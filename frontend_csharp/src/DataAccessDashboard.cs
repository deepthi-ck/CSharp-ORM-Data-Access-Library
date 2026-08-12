using System;
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
            _client = client;
            _version = version;
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
    }
}
