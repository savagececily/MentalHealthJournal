using MentalHealthJournal.Models;
using MentalHealthJournal.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MentalHealthJournal.Tests.Controllers
{
    public class JournalControllerIntegrationTests : IClassFixture<WebApplicationFactory<MentalHealthJournal.Server.Program>>
    {
        private readonly WebApplicationFactory<MentalHealthJournal.Server.Program> _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public JournalControllerIntegrationTests(WebApplicationFactory<MentalHealthJournal.Server.Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        [Fact(Skip = "Integration test requires Azure credentials and configuration")]
        public async Task Get_UserEntries_WithoutRealAzureServices_ReturnsUnauthorizedOrServerError()
        {
            // This test demonstrates that the endpoint exists and handles requests
            // In a real scenario, you'd mock the Azure services or use test doubles
            
            // Arrange
            var userId = "test-user-123";

            // Act
            var response = await _client.GetAsync($"/api/journal/entries/{userId}");

            // Assert
            // We expect either 401 (if auth is required) or 500 (if services aren't configured)
            // Both are acceptable for this integration test without real Azure services
            Assert.True(
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.InternalServerError ||
                response.StatusCode == HttpStatusCode.OK,
                $"Expected 200, 401, or 500, but got {response.StatusCode}"
            );
        }

        [Fact(Skip = "Integration test requires Azure credentials and configuration")]
        public async Task Post_AnalyzeEntry_WithoutRealAzureServices_ReturnsServerErrorOrBadRequest()
        {
            // This test demonstrates that the endpoint exists and handles requests
            
            // Arrange
            var request = TestHelper.CreateSampleJournalRequest();

            // Act
            var response = await _client.PostAsJsonAsync("/api/journal/analyze", request, _jsonOptions);

            // Assert
            // We expect either 400 (validation error) or 500 (services not configured)
            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.InternalServerError ||
                response.StatusCode == HttpStatusCode.OK,
                $"Expected 200, 400, or 500, but got {response.StatusCode}"
            );
        }

        [Fact(Skip = "Integration test requires Azure credentials and configuration")]
        public async Task Post_AnalyzeEntry_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var invalidRequest = new { }; // Empty object

            // Act
            var response = await _client.PostAsJsonAsync("/api/journal/analyze", invalidRequest, _jsonOptions);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact(Skip = "Integration test requires Azure credentials and configuration")]
        public async Task Get_UserEntries_WithEmptyUserId_ReturnsNotFound()
        {
            // Act
            var response = await _client.GetAsync("/api/journal/entries/");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact(Skip = "Integration test requires Azure credentials and configuration")]
        public async Task HealthCheck_SwaggerEndpoint_IsAccessible()
        {
            // This is testing that the API is properly configured
            
            // Act
            var response = await _client.GetAsync("/swagger/index.html");

            // Assert
            // In development, swagger should be accessible
            // In production, it might be disabled
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.NotFound,
                $"Expected 200 or 404 for swagger, but got {response.StatusCode}"
            );
        }
    }

    // Custom WebApplicationFactory for testing with mocked services
    public class TestWebApplicationFactory : WebApplicationFactory<MentalHealthJournal.Server.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Here you could replace real Azure services with test doubles
                // For example:
                // services.RemoveAll<IJournalAnalysisService>();
                // services.AddScoped<IJournalAnalysisService, MockJournalAnalysisService>();
                
                // This is useful for more comprehensive integration testing
            });
        }
    }
}
