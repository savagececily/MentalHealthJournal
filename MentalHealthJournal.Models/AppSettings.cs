using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentalHealthJournal.Models
{
    public class AppSettings
    {
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
        public string ContainerName { get; set; } = string.Empty;
    }

    public class CosmosDbSettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string JournalEntryContainer { get; set; } = string.Empty;
    }
}
