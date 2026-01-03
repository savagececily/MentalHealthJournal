# 🚀 Manual Azure Deployment Steps

## ✅ Build Complete!

Your application has been successfully built and packaged:

- **Frontend**: Built and copied to `wwwroot/`
- **Backend**: Published to `publish/` folder
- **Deployment Package**: `MentalHealthJournal-deploy.zip` (73MB)

---

## 🎯 Deploy via Azure Portal (Recommended)

Since you're hitting quota limits with Azure CLI, use the Azure Portal instead:

### Step 1: Create App Service in Portal

1. **Go to Azure Portal**: https://portal.azure.com
2. **Click "Create a resource"**
3. **Search for "Web App"** and click Create
4. **Fill in the details**:
   - **Subscription**: Visual Studio Enterprise Subscription
   - **Resource Group**: `MentalHealthJournal` (select existing)
   - **Name**: `mentalhealthjournal-app` (or your preferred name)
   - **Publish**: Code
   - **Runtime stack**: .NET 8 (LTS)
   - **Operating System**: Linux
   - **Region**: East US
   - **Pricing tier**: Click "Change size"
     - Try **Free F1** first (1GB RAM, 60 min/day compute)
     - If quota error, try **Basic B1** ($13/month)
     - Request quota increase if needed

5. **Click "Review + Create"** then **"Create"**

### Step 2: Deploy Using VS Code (Easiest)

1. **Install Extension**:
   - Open VS Code
   - Install "Azure App Service" extension
   - Sign in to Azure

2. **Deploy**:
   - In VS Code, click Azure icon in sidebar
   - Find your subscription
   - Right-click your new app service
   - Select "Deploy to Web App..."
   - Choose the `/MentalHealthJournal.Server/publish` folder
   - Confirm deployment

### Step 3: Deploy Using Azure Portal

1. **Go to your App Service** in Azure Portal
2. **Click "Deployment Center"** (left menu)
3. **Select "ZIP Deploy"**
4. **Upload** `/Users/cecilysavage/GitHub/MentalHealthJournal/MentalHealthJournal.Server/MentalHealthJournal-deploy.zip`
5. **Wait for deployment** to complete

### Step 4: Configure App Settings

1. **In Azure Portal, go to your App Service**
2. **Click "Environment variables"** (left menu)
3. **Add these App settings** (click "+ Add"):

```
ASPNETCORE_ENVIRONMENT = Production

AzureAppConfiguration = https://MentalHealthJournal-AppConfig.azconfig.io

AzureCognitiveServices__Endpoint = https://mentalhealthjournal-cogservices.cognitiveservices.azure.com/
AzureCognitiveServices__Key = [Your Cognitive Services Key]
AzureCognitiveServices__Region = eastus

AzureOpenAI__Endpoint = https://MentalHealthJournal-OpenAI.openai.azure.com/
AzureOpenAI__Key = [Your OpenAI Key]  
AzureOpenAI__DeploymentName = mentalhealthjournal-gpt-4

CosmosDb__Endpoint = https://mentalhealthjournal-cosmosdb.documents.azure.com:443/
CosmosDb__Key = [Your Cosmos DB Key]
CosmosDb__DatabaseName = MentalHealthJournal
CosmosDb__JournalEntryContainer = JournalEntries

AzureBlobStorage__ConnectionString = [Your Blob Storage Connection String]
AzureBlobStorage__ContainerName = journalaudio
```

4. **Click "Apply"** at the bottom
5. **Restart the app**

### Step 5: Get Your Connection Strings & Keys

Run these commands to get the values:

```bash
# Cognitive Services Key
az cognitiveservices account keys list \
  --name MentalHealthJournal-CogServices \
  --resource-group MentalHealthJournal \
  --query key1 -o tsv

# Azure OpenAI Key
az cognitiveservices account keys list \
  --name MentalHealthJournal-OpenAI \
  --resource-group MentalHealthJournal \
  --query key1 -o tsv

# Cosmos DB Key
az cosmosdb keys list \
  --name mentalhealthjournal-cosmosdb \
  --resource-group MentalHealthJournal \
  --query primaryMasterKey -o tsv

# Blob Storage Connection String
az storage account show-connection-string \
  --name samentalhealthjournal \
  --resource-group MentalHealthJournal \
  --query connectionString -o tsv
```

### Step 6: Verify Deployment

1. **In Portal, click "Browse"** (top of App Service page)
2. **Your app should load** at: `https://mentalhealthjournal-app.azurewebsites.net`
3. **Test creating a journal entry**

---

## 🔒 Enable Managed Identity (More Secure)

After deployment works with keys, switch to Managed Identity:

### Step 1: Enable Identity

1. **In Portal, go to your App Service**
2. **Click "Identity"** (left menu)
3. **System assigned** tab
4. **Turn Status to "On"**
5. **Save** and copy the Object (principal) ID

### Step 2: Grant Permissions

```bash
# Get the principal ID
PRINCIPAL_ID="[Paste the Object ID from Portal]"

# Grant Cosmos DB access
az cosmosdb sql role assignment create \
  --account-name mentalhealthjournal-cosmosdb \
  --resource-group MentalHealthJournal \
  --principal-id $PRINCIPAL_ID \
  --role-definition-name "Cosmos DB Built-in Data Contributor" \
  --scope "/"

# Grant Cognitive Services access
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Cognitive Services User" \
  --scope /subscriptions/a7c4f882-34af-44dc-9bd7-ccac4f1ec402/resourceGroups/MentalHealthJournal/providers/Microsoft.CognitiveServices/accounts/MentalHealthJournal-CogServices

# Grant OpenAI access
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Cognitive Services OpenAI User" \
  --scope /subscriptions/a7c4f882-34af-44dc-9bd7-ccac4f1ec402/resourceGroups/MentalHealthJournal/providers/Microsoft.CognitiveServices/accounts/MentalHealthJournal-OpenAI

# Grant Blob Storage access
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Storage Blob Data Contributor" \
  --scope /subscriptions/a7c4f882-34af-44dc-9bd7-ccac4f1ec402/resourceGroups/MentalHealthJournal/providers/Microsoft.Storage/storageAccounts/samentalhealthjournal
```

### Step 3: Update App Settings

Remove the `__Key` settings and your app will automatically use Managed Identity!

---

## 📊 Monitor Your App

### View Logs

1. **In Portal, go to "Log stream"** (left menu)
2. **See real-time logs** as users interact with your app

### Application Insights

1. **Click "Application Insights"** (left menu)
2. **Enable** if not already enabled
3. **View performance**, errors, and usage

---

## 🆘 Troubleshooting

### Error: "Quota exceeded"

**Solution**: Request quota increase in Portal:
1. Go to **Subscriptions** → Your subscription
2. Click **Usage + quotas**
3. Search for "App Service"
4. Click pencil icon to request increase

OR try deploying to a different region:
- West US 2
- West Europe
- Southeast Asia

### Error: "502 Bad Gateway"

**Check**:
1. App Settings are configured
2. Azure services are accessible
3. View logs in Log Stream

### Frontend not loading

**Check**:
1. `wwwroot/` folder exists in deployment
2. Contains `index.html` and `assets/` folder
3. No errors in browser console

---

## ✅ Deployment Checklist

- [ ] App Service created in Azure Portal
- [ ] Deployment zip uploaded (73MB)
- [ ] App Settings configured (8 settings)
- [ ] Cosmos DB database `MentalHealthJournal` exists
- [ ] Cosmos DB container `JournalEntries` exists (partition key: `/userId`)
- [ ] Blob Storage container `journalaudio` exists
- [ ] App loads at `https://[your-app].azurewebsites.net`
- [ ] Can create journal entries
- [ ] AI analysis works (sentiment, affirmation, etc.)
- [ ] Managed Identity enabled (optional but recommended)

---

## 🎉 You're Ready!

Your Mental Health Journal is packaged and ready for Azure!

**Deployment Package Location**:
```
/Users/cecilysavage/GitHub/MentalHealthJournal/MentalHealthJournal.Server/MentalHealthJournal-deploy.zip
```

Use Azure Portal to create the App Service and upload this zip file.
