using OrmDataAccess.Shared;
using Xunit;

namespace OrmDataAccess.Backend.Tests
{
    public class DataAccessServiceTest
    {
        [Fact]
        public void PutGetDelete_RoundTrip()
        {
            var (service, _, _, _) = TestHost.Create();
            var put = service.Put(new DataAccessRequest { Key = "user:1001", Value = "Visvantha", Kind = "user" });
            Assert.True(put.Success);
            Assert.Equal("Visvantha", service.Get("user:1001").Value);
            Assert.True(service.Delete("user:1001").Success);
            Assert.Equal("not_found", service.Get("user:1001").Status);
        }
    }
}
