
using MentalHealthJournal.Services;
using Azure;
using Azure.Core;
using Microsoft.Extensions.Azure;
using Azure.Identity;
using Azure.AI.TextAnalytics;
using OpenAI;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;

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
                Uri configurationUri = new Uri(config["AzureAppConfiguration"]);
                options.Connect(configurationUri, defaultCredential);
            });

            // === Azure OpenAI ===
            builder.Services.AddSingleton(_ =>
            {
                var endpoint = new Uri(config["AzureOpenAI:Endpoint"]);
                var key = new AzureKeyCredential(config["AzureOpenAI:Key"]);
                return new AzureOpenAIClient(endpoint, key);
            });

            builder.Services.AddAzureClients(clients =>
            {
                clients.AddBlobServiceClient(
                    config["AzureBlobStorage:ConnectionString"])
                    .WithCredential(defaultCredential);

                clients.AddTextAnalyticsClient(
                    new Uri(config["AzureCognitiveServices:Endpoint"]), new AzureKeyCredential(config["AzureCognitiveServices:Key"]));
            });

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
