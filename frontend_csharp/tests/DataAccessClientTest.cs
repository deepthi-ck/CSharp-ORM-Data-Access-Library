using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OrmDataAccess.Frontend;
using OrmDataAccess.Shared;
using Xunit;

namespace OrmDataAccess.Frontend.Tests
{
    public class DataAccessClientTest
    {
        [Fact]
        public async Task PutGet_UsesHttpClient()
        {
            var handler = new StubHandler();
            var client = new DataAccessClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5084/") });
            var put = await client.PutAsync("user:1001", "Visvantha");
            Assert.True(put!.Success);
            Assert.Equal("Visvantha", put.Value);
            var get = await client.GetAsync("user:1001");
            Assert.Equal("Visvantha", get!.Value);
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var body = JsonSerializer.Serialize(new DataAccessResponse
                {
                    Success = true, Status = "success", Message = "ok",
                    Key = "user:1001", Value = "Visvantha", Found = true,
                    Entry = new EntityEntry { Key = "user:1001", Value = "Visvantha" }
                });
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                });
            }
        }
    }
}
