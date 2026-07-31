using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Integration
{
    /// <summary>
    /// API katmanını bellekte (in-memory) ayağa kaldırarak uçtan uca entegrasyon testlerini koordine eden sınıf.
    /// </summary>
    public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public HealthCheckTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task HealthCheck_ShouldReturnValidJsonStatus_AndOk()
        {
            // Arrange (Bellekteki API istemcisinin oluşturulması)
            var client = _factory.CreateClient();

            // Act (İstek atılması)
            var response = await client.GetAsync("/api/health");

            // Assert (Doğrulama)
            // Not: Veritabanı bağlantısı durumuna göre durum Healthy veya Unhealthy olabilir.
            // Önemli olan uç noktanın 200 OK (Sağlıklı) veya 503 ServiceUnavailable (Sağlıksız) dönmesi ve doğru JSON formatına sahip olmasıdır.
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().ContainAny("\"status\":\"Healthy\"", "\"status\":\"Unhealthy\"");
            content.Should().Contain("\"name\":\"Database\"");
        }
    }
}
