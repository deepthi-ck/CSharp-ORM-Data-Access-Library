using Xunit;

namespace OrmDataAccess.Backend.Tests
{
    public class ReplicationManagerTest
    {
        [Fact]
        public void Put_Replicates_And_PreferReplica_Reads()
        {
            var (_, uow, replication, _) = TestHost.Create();
            uow.Put("user:1001", "Visvantha");
            var replica = replication.GetReplica("user:1001");
            Assert.NotNull(replica);
            Assert.Equal("Visvantha", replica!.Value);
            var viaPrefer = uow.Get("user:1001", preferReplica: true);
            Assert.Equal("Visvantha", viaPrefer!.Value);
        }
    }
}
