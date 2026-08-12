using System;
using OrmDataAccess.Persistence;
using OrmDataAccess.Shared;

namespace OrmDataAccess.Backend
{
    public sealed class DataAccessService
    {
        private readonly UnitOfWorkManager _uow;
        private readonly DataAccessStatistics _stats;
        private readonly bool _healthy;

        public DataAccessService(UnitOfWorkManager uow, DataAccessStatistics stats, bool healthy = true)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _stats = stats;
            _healthy = healthy;
        }

        public DataAccessResponse Put(DataAccessRequest request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Key) || request.Value is null)
                return Fail("put", "invalid_request");
            var entry = _uow.Put(request.Key, request.Value, string.IsNullOrWhiteSpace(request.Kind) ? "entity" : request.Kind!);
            return Ok("put", "PUT = SUCCESS", entry);
        }

        public DataAccessResponse Get(string key, bool preferReplica = false)
        {
            if (string.IsNullOrWhiteSpace(key)) return Fail("get", "invalid_request");
            var entry = _uow.Get(key, preferReplica);
            return entry is null ? NotFound(key) : Ok("get", "GET = SUCCESS", entry);
        }

        public DataAccessResponse Delete(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return Fail("delete", "invalid_request");
            var ok = _uow.Delete(key);
            return ok
                ? new DataAccessResponse { Success = true, Status = "success", Operation = "delete", Message = "DELETE = SUCCESS", Key = key, Found = false }
                : NotFound(key);
        }

        public object Stats() => _stats.Snapshot();
        public object Health() => new
        {
            status = _healthy ? "healthy" : "degraded",
            orm = _healthy ? "available" : "unavailable",
            nodes = _stats.NodeCount
        };

        private static DataAccessResponse Ok(string op, string message, EntityEntry e) => new DataAccessResponse
        {
            Success = true, Status = "success", Operation = op, Message = message,
            Key = e.Key, Value = e.Value, Kind = e.Kind, NodeSlot = e.NodeSlot,
            Found = true, Revision = e.Revision, Entry = e
        };
        private static DataAccessResponse Fail(string op, string reason) => new DataAccessResponse
        { Success = false, Status = "error", Operation = op, Message = reason, Found = false };
        private static DataAccessResponse NotFound(string key) => new DataAccessResponse
        { Success = false, Status = "not_found", Operation = "get", Message = "NOT_FOUND", Key = key, Found = false };
    }
}
