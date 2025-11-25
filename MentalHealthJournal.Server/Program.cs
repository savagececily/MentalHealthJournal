
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

            var config = builder.Configuration;

            var defaultCredential = new DefaultAzureCredential();

            builder.Configuration.AddAzureAppConfiguration(options =>
            {
                var configurationUri = config["AzureAppConfiguration"] ?? throw new InvalidOperationException("AzureAppConfiguration is not configured");
                options.Connect(new Uri(configurationUri), defaultCredential);
            });

            // === Azure OpenAI ===
            builder.Services.AddSingleton(_ =>
            {
                var endpointString = config["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured");
                var keyString = config["AzureOpenAI:Key"] ?? throw new InvalidOperationException("AzureOpenAI:Key is not configured");
                
                var endpoint = new Uri(endpointString);
                var key = new AzureKeyCredential(keyString);
                return new AzureOpenAIClient(endpoint, key);
            });

            builder.Services.AddAzureClients(clients =>
            {
                var blobConnectionString = config["AzureBlobStorage:ConnectionString"] ?? throw new InvalidOperationException("AzureBlobStorage:ConnectionString is not configured");
                clients.AddBlobServiceClient(blobConnectionString)
                    .WithCredential(defaultCredential);

                var cognitiveEndpoint = config["AzureCognitiveServices:Endpoint"] ?? throw new InvalidOperationException("AzureCognitiveServices:Endpoint is not configured");
                var cognitiveKey = config["AzureCognitiveServices:Key"] ?? throw new InvalidOperationException("AzureCognitiveServices:Key is not configured");
                clients.AddTextAnalyticsClient(new Uri(cognitiveEndpoint), new AzureKeyCredential(cognitiveKey));
            });

            // === Cosmos DB ===
            builder.Services.AddSingleton<CosmosClient>(serviceProvider =>
            {
                var endpoint = config["CosmosDb:Endpoint"] ?? throw new InvalidOperationException("CosmosDb:Endpoint is not configured");
                var key = config["CosmosDb:Key"] ?? throw new InvalidOperationException("CosmosDb:Key is not configured");
                return new CosmosClient(endpoint, key);
            });

            // === Configuration ===
            builder.Services.Configure<AppSettings>(config);

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

            app.Run();
        }
    }
}
