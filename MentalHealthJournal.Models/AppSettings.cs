
namespace MentalHealthJournal.Models
{
    public class AppSettings
    {
        public string AzureAppConfiguration { get; set; } = string.Empty;
        public string ManagedIdentityClientId { get; set; } = string.Empty;
        public AzureCognitiveServicesSettings AzureCognitiveServices { get; set; } = new();
        public AzureOpenAISettings AzureOpenAI { get; set; } = new();
        public AzureBlobStorageSettings AzureBlobStorage { get; set; } = new();
        public CosmosDbSettings CosmosDb { get; set; } = new();
    }

    public class AzureCognitiveServicesSettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
    }

    public class AzureOpenAISettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string DeploymentName { get; set; } = string.Empty;
    }

    public class AzureBlobStorageSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ServiceUri { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
    }

    public class CosmosDbSettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string JournalEntryContainer { get; set; } = string.Empty;
        public string UserContainer { get; set; } = string.Empty;
    }
}
