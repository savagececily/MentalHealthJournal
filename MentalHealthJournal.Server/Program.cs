
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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace MentalHealthJournal.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure logging
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();

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

            // Add Application Insights telemetry with explicit connection string
            var appInsightsConnectionString = config["APPLICATIONINSIGHTS_CONNECTION_STRING"];
            if (!string.IsNullOrEmpty(appInsightsConnectionString))
            {
                builder.Services.AddApplicationInsightsTelemetry(options =>
                {
                    options.ConnectionString = appInsightsConnectionString;
                });
                builder.Logging.AddApplicationInsights(
                    configureTelemetryConfiguration: (config) => config.ConnectionString = appInsightsConnectionString,
                    configureApplicationInsightsLoggerOptions: (options) => { }
                );
            }
            else
            {
                Console.WriteLine("WARNING: Application Insights connection string not found!");
            }

            builder.Services.AddLogging();

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
                var cosmosClientOptions = new CosmosClientOptions
                {
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    }
                };
                return new CosmosClient(endpoint, defaultCredential, cosmosClientOptions);
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
            builder.Services.AddSingleton<IUserService, UserService>();
            builder.Services.AddSingleton<IDataExportService, DataExportService>();

            // === JWT Authentication ===
            var jwtKey = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured");
            var jwtIssuer = config["Jwt:Issuer"] ?? "MentalHealthJournal";
            var jwtAudience = config["Jwt:Audience"] ?? "MentalHealthJournalApp";

            // Validate JWT key length for security (minimum 256 bits/32 bytes for HS256)
            var jwtKeyBytes = Encoding.UTF8.GetBytes(jwtKey);
            if (jwtKeyBytes.Length < 32)
            {
                throw new InvalidOperationException("JWT Key must be at least 256 bits (32 bytes) for secure HS256 signing. Please use a longer key.");
            }

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

            builder.Services.AddAuthorization();

            // Add CORS policy
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(
                        "http://localhost:54551",
                        "http://localhost:5173",
                        "https://localhost:54551",
                        "https://localhost:5173",
                        "https://mentalhealthjournal-webapp.azurewebsites.net"
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
                });
            });

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("======================================");
            logger.LogInformation("Mental Health Journal Application Starting");
            logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
            
            var appInsightsConnString = config["APPLICATIONINSIGHTS_CONNECTION_STRING"];
            logger.LogInformation("Application Insights: {Status}", 
                string.IsNullOrEmpty(appInsightsConnString) ? "NOT CONFIGURED" : "CONFIGURED");
            
            // Send a test telemetry event
            if (!string.IsNullOrEmpty(appInsightsConnString))
            {
                logger.LogInformation("Application Insights telemetry is enabled");
            }
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

            app.UseCors("AllowFrontend");

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.MapFallbackToFile("/index.html");

            logger.LogInformation("Application started successfully");
            
            app.Run();
        }
    }
}
