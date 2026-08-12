using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OrmDataAccess.Backend.Tests
{
    public class UnitOfWorkManagerTest
    {
        [Fact]
        public void Statistics_ReflectOperations()
        {
            var (_, uow, _, _) = TestHost.Create();
            uow.Put("user:1", "A");
            uow.Get("user:1");
            uow.Get("missing");
            uow.Delete("user:1");
            Assert.Equal(1, uow.Statistics.PutCount);
            Assert.Equal(2, uow.Statistics.GetCount);
            Assert.Equal(1, uow.Statistics.HitCount);
            Assert.Equal(1, uow.Statistics.MissCount);
            Assert.Equal(1, uow.Statistics.DeleteCount);
        }

        [Fact]
        public async Task Ttl_ExpiresEntry()
        {
            var (_, uow, _, _) = TestHost.Create(ttlSeconds: 1);
            uow.Put("user:ttl", "Visvantha");
            await Task.Delay(1500);
            Assert.Null(uow.Get("user:ttl"));
        }

        [Fact]
        public void Eviction_RespectsCapacity()
        {
            var (_, uow, _, stores) = TestHost.Create(maxEntries: 2);
            uow.Put("a:1", "a");
            uow.Put("b:1", "b");
            uow.Put("c:1", "c");
            Assert.True(stores.Sum(s => s.Count) <= 2);
            Assert.NotNull(uow.Get("c:1"));
        }
    }
}
