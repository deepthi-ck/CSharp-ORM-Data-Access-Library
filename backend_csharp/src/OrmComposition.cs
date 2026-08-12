using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using OrmDataAccess.Distribution;
using OrmDataAccess.Persistence;
using OrmDataAccess.Shared;

namespace OrmDataAccess.Backend
{
    public sealed class OrmComposition
    {
        public DataAccessConfiguration Config { get; }
        public DataAccessService Service { get; }
        public UnitOfWorkManager UnitOfWork { get; }
        public DataAccessStatistics Stats { get; }
        public VersionInfo Version { get; }

        public OrmComposition(DataAccessConfiguration cfg, VersionInfo version)
        {
            Config = cfg;
            Version = version;
            var policy = PersistencePolicy.FromConfiguration(cfg);
            var stores = new List<InMemoryStore>();
            var registry = new NodeRegistry();
            for (var i = 0; i < cfg.NodeSlotCount; i++)
            {
                var store = new InMemoryStore();
                stores.Add(store);
                registry.Register(new StoreNode(i, store, policy));
            }
            Stats = new DataAccessStatistics();
            Stats.SetNodeCount(registry.Count);
            var router = new PartitionRouter(registry, cfg.NodeSlotCount);
            var replication = new ReplicationManager();
            var eviction = new EvictionManager(stores, policy);
            var expiration = new ExpirationManager(stores, replication);
            var db = new ApplicationDbContext(router, replication, stores);
            UnitOfWork = new UnitOfWorkManager(db, Stats, eviction, expiration);
            Service = new DataAccessService(UnitOfWork, Stats, healthy: true);
        }

        public void SeedIfRequested()
        {
            if (!string.Equals(Environment.GetEnvironmentVariable("SEED_SAMPLE_DATA"), "1", StringComparison.Ordinal))
                return;
            foreach (var item in LoadSample(Config.SampleDataPath))
                UnitOfWork.Put(item.Key, item.Value, item.Kind);
        }

        public static List<(string Key, string Value, string Kind)> LoadSample(string path)
        {
            var list = new List<(string, string, string)>();
            var full = Path.IsPathRooted(path) ? path : Path.Combine(Directory.GetCurrentDirectory(), path);
            if (!File.Exists(full)) return list;
            using var doc = JsonDocument.Parse(File.ReadAllText(full));
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                list.Add((
                    el.GetProperty("key").GetString()!,
                    el.GetProperty("value").GetString()!,
                    el.TryGetProperty("kind", out var k) ? k.GetString() ?? "entity" : "entity"));
            }
            return list;
        }
    }
}
