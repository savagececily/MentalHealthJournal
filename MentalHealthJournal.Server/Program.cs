
using MentalHealthJournal.Services;
using MentalHealthJournal.Models;
using Azure;
using Azure.Core;
using Microsoft.Extensions.Azure;
using Azure.Identity;
using Azure.AI.TextAnalytics;
using OpenAI;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;

namespace MentalHealthJournal.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Information);

            // Add Application Insights telemetry
            builder.Services.AddApplicationInsightsTelemetry();

            var defaultCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = Environment.GetEnvironmentVariable("ManagedIdentityClientId")
            });

            // Load Azure App Configuration FIRST before accessing other config values
            var configurationUri = Environment.GetEnvironmentVariable("AzureAppConfiguration") ?? throw new InvalidOperationException("AzureAppConfiguration is not configured");
            builder.Configuration.AddAzureAppConfiguration(options =>
            {
                options.Connect(new Uri(configurationUri), defaultCredential);
            });

            // Rebuild configuration to include App Configuration values
            var config = builder.Configuration;

            // === Azure OpenAI with Managed Identity ===
            builder.Services.AddSingleton(_ =>
            {
                var endpointString = config["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured");
                var endpoint = new Uri(endpointString);
                return new AzureOpenAIClient(endpoint, defaultCredential);
            });

            builder.Services.AddAzureClients(clients =>
            {
                // Use Blob Storage with Managed Identity
                var blobServiceUri = config["AzureBlobStorage:ServiceUri"] ?? throw new InvalidOperationException("AzureBlobStorage:ServiceUri is not configured");
                clients.AddBlobServiceClient(new Uri(blobServiceUri))
                    .WithCredential(defaultCredential);
            });

            // === Text Analytics with Managed Identity ===
            builder.Services.AddSingleton<TextAnalyticsClient>(serviceProvider =>
            {
                var cognitiveEndpoint = config["AzureCognitiveServices:Endpoint"] ?? throw new InvalidOperationException("AzureCognitiveServices:Endpoint is not configured");
                return new TextAnalyticsClient(new Uri(cognitiveEndpoint), defaultCredential);
            });

            // === Cosmos DB with Managed Identity ===
            builder.Services.AddSingleton<CosmosClient>(serviceProvider =>
            {
                var endpoint = config["CosmosDb:Endpoint"] ?? throw new InvalidOperationException("CosmosDb:Endpoint is not configured");
                return new CosmosClient(endpoint, defaultCredential);
            });

            // === Configuration ===
            builder.Services.AddOptions<AppSettings>()
            .Bind(builder.Configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

            // Add services to the container.
            builder.Services.AddScoped<IJournalAnalysisService, JournalAnalysisService>();
            builder.Services.AddSingleton<ISpeechToTextService, SpeechToTextService>();
            builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
            builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("======================================");
            logger.LogInformation("Mental Health Journal Application Starting");
            logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
            logger.LogInformation("Application Insights Enabled: {Enabled}", !string.IsNullOrEmpty(config["APPLICATIONINSIGHTS_CONNECTION_STRING"]));
            logger.LogInformation("======================================");

            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.MapFallbackToFile("/index.html");

            logger.LogInformation("Application started successfully");
            
            app.Run();
        }
    }
}
