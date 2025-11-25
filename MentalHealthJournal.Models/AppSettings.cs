using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentalHealthJournal.Models
{
    public class AppSettings
    {
        public AzureCongnitiveServicesSettings AzureCongitiveServices { get; set; }
        public AzureOpenAISettings AzureOpenAI { get; set; }
        public AzureBlobStorage AzureBlobStorage { get; set; }
        public CosmosDbSettings CosmosDb { get; set; }

    }

    public class AzureCongnitiveServicesSettings
    {
        public string Endpoint { get; set; }
        public string Key { get; set; }
        public string Region { get; set; }

    }

    public class AzureOpenAISettings
    {
        public string Endpoint { get; set; }
        public string Key { get; set; }
        public string DeploymentName { get; set; }
    }

    public class AzureBlobStorage
    {
        public string ConnectionString { get; set; }
        public string JournalContainer { get; set; }
    }

    public class CosmosDbSettings
    {
        public string Endpoint { get; set; }
        public string Key { get; set; }
        public string DatabaseName { get; set; }
        public string JournalEntryContainer { get; set; }
    }
}
