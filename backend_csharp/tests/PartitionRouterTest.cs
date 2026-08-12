using OrmDataAccess.Distribution;
using OrmDataAccess.Persistence;
using Xunit;

namespace OrmDataAccess.Backend.Tests
{
    public class PartitionRouterTest
    {
        [Fact]
        public void SameKey_RoutesConsistently()
        {
            var policy = new PersistencePolicy { NodeSlotCount = 3 };
            var registry = new NodeRegistry();
            for (var i = 0; i < 3; i++) registry.Register(new StoreNode(i, new InMemoryStore(), policy));
            var router = new PartitionRouter(registry, 3);
            var a = router.ResolveSlot("user:1001");
            var b = router.ResolveSlot("user:1001");
            Assert.Equal(a, b);
            Assert.InRange(a, 0, 2);
        }
    }
}
