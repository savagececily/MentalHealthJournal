# 🚀 Deploy Mental Health Journal to Azure

## Prerequisites

✅ Azure subscription  
✅ Azure CLI installed ([Install here](https://learn.microsoft.com/cli/azure/install-azure-cli))  
✅ .NET 8 SDK  
✅ Node.js 22+

## Deployment Options

### Option 1: Azure App Service (Recommended for Full-Stack Apps)
### Option 2: Azure Static Web Apps + Functions
### Option 3: Azure Container Apps

---

## 🎯 Option 1: Azure App Service (Easiest)

This deploys your entire .NET backend + React frontend as a single Azure App Service.

### Step 1: Login to Azure

```bash
az login
az account set --subscription "YOUR_SUBSCRIPTION_NAME"
```

### Step 2: Create Resource Group

```bash
# Create resource group
az group create \
  --name rg-mentalhealthjournal \
  --location eastus
```

### Step 3: Create App Service Plan

```bash
# Create App Service Plan (B1 = Basic tier, good for development)
az appservice plan create \
  --name plan-mentalhealthjournal \
  --resource-group rg-mentalhealthjournal \
  --sku B1 \
  --is-linux
```

### Step 4: Create Web App

```bash
# Create Web App with .NET 8 runtime
az webapp create \
  --name mentalhealthjournal-app \
  --resource-group rg-mentalhealthjournal \
  --plan plan-mentalhealthjournal \
  --runtime "DOTNET:8.0"
```

### Step 5: Configure App Settings

```bash
# Set environment to Production
az webapp config appsettings set \
  --name mentalhealthjournal-app \
  --resource-group rg-mentalhealthjournal \
  --settings ASPNETCORE_ENVIRONMENT=Production

# Add Azure service configurations
az webapp config appsettings set \
  --name mentalhealthjournal-app \
  --resource-group rg-mentalhealthjournal \
  --settings \
    AzureAppConfiguration="YOUR_APP_CONFIG_URL" \
    AzureCognitiveServices__Endpoint="YOUR_COGNITIVE_ENDPOINT" \
    AzureCognitiveServices__Key="YOUR_COGNITIVE_KEY" \
    AzureOpenAI__Endpoint="YOUR_OPENAI_ENDPOINT" \
    AzureOpenAI__Key="YOUR_OPENAI_KEY" \
    AzureOpenAI__DeploymentName="YOUR_DEPLOYMENT_NAME" \
    CosmosDb__Endpoint="YOUR_COSMOS_ENDPOINT" \
    CosmosDb__Key="YOUR_COSMOS_KEY" \
    CosmosDb__DatabaseName="MentalHealthJournal" \
    CosmosDb__JournalEntryContainer="JournalEntries" \
    AzureBlobStorage__ConnectionString="YOUR_BLOB_CONNECTION_STRING" \
    AzureBlobStorage__ContainerName="journalaudio"
```

### Step 6: Build and Publish

```bash
# Navigate to server directory
cd MentalHealthJournal.Server

# Build frontend first
cd ../mentalhealthjournal.client
npm install
npm run build

# Copy build to backend wwwroot
mkdir -p ../MentalHealthJournal.Server/wwwroot
cp -r dist/* ../MentalHealthJournal.Server/wwwroot/

# Publish .NET app
cd ../MentalHealthJournal.Server
dotnet publish -c Release -o ./publish
```

### Step 7: Deploy to Azure

#### Option A: Using Azure CLI (Recommended)

```bash
# Create a zip file
cd publish
zip -r ../deploy.zip .
cd ..

# Deploy the zip file
az webapp deployment source config-zip \
  --name mentalhealthjournal-app \
  --resource-group rg-mentalhealthjournal \
  --src deploy.zip
```

#### Option B: Using Visual Studio Code

1. Install "Azure App Service" extension
2. Right-click `MentalHealthJournal.Server` project
3. Select "Deploy to Web App..."
4. Choose your subscription and app service
5. Confirm deployment

#### Option C: Using GitHub Actions (CI/CD)

See **Continuous Deployment** section below.

### Step 8: Verify Deployment

```bash
# Get the app URL
az webapp show \
  --name mentalhealthjournal-app \
  --resource-group rg-mentalhealthjournal \
  --query defaultHostName -o tsv
```

Visit: `https://mentalhealthjournal-app.azurewebsites.net`

### Step 9: Enable HTTPS Only

```bash
az webapp update \
  --name mentalhealthjournal-app \
  --resource-group rg-mentalhealthjournal \
  --https-only true
```

---

## 🔐 Using Azure Managed Identity (More Secure)

Instead of storing keys in app settings, use Managed Identity:

### Step 1: Enable Managed Identity

```bash
az webapp identity assign \
  --name mentalhealthjournal-app \
  --resource-group rg-mentalhealthjournal
```

### Step 2: Grant Permissions to Azure Services

```bash
# Get the managed identity principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name mentalhealthjournal-app \
  --resource-group rg-mentalhealthjournal \
  --query principalId -o tsv)

# Grant access to Cosmos DB
az cosmosdb sql role assignment create \
  --account-name YOUR_COSMOS_ACCOUNT \
  --resource-group rg-mentalhealthjournal \
  --principal-id $PRINCIPAL_ID \
  --role-definition-name "Cosmos DB Built-in Data Contributor" \
  --scope "/"

# Grant access to Cognitive Services
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Cognitive Services User" \
  --scope /subscriptions/YOUR_SUBSCRIPTION_ID/resourceGroups/rg-mentalhealthjournal

# Grant access to Storage Account
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Storage Blob Data Contributor" \
  --scope /subscriptions/YOUR_SUBSCRIPTION_ID/resourceGroups/rg-mentalhealthjournal/providers/Microsoft.Storage/storageAccounts/YOUR_STORAGE_ACCOUNT
```

### Step 3: Update Code to Use DefaultAzureCredential

Your code already uses `DefaultAzureCredential`, so it will automatically use Managed Identity in Azure!

---

## 🔄 Continuous Deployment with GitHub Actions

### Step 1: Get Publish Profile

```bash
az webapp deployment list-publishing-profiles \
  --name mentalhealthjournal-app \
  --resource-group rg-mentalhealthjournal \
  --xml > publishprofile.xml
```

### Step 2: Add GitHub Secret

1. Go to your GitHub repository
2. Settings → Secrets and variables → Actions
3. New repository secret
4. Name: `AZURE_WEBAPP_PUBLISH_PROFILE`
5. Value: Paste contents of `publishprofile.xml`

### Step 3: Create GitHub Actions Workflow

Create `.github/workflows/azure-deploy.yml`:

```yaml
name: Deploy to Azure App Service

on:
  push:
    branches: [ main ]
  workflow_dispatch:

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Setup Node.js
      uses: actions/setup-node@v4
      with:
        node-version: '22'
    
    - name: Build Frontend
      run: |
        cd mentalhealthjournal.client
        npm install
        npm run build
        mkdir -p ../MentalHealthJournal.Server/wwwroot
        cp -r dist/* ../MentalHealthJournal.Server/wwwroot/
    
    - name: Build Backend
      run: |
        cd MentalHealthJournal.Server
        dotnet restore
        dotnet build --configuration Release
        dotnet publish -c Release -o ./publish
    
    - name: Deploy to Azure Web App
      uses: azure/webapps-deploy@v2
      with:
        app-name: 'mentalhealthjournal-app'
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./MentalHealthJournal.Server/publish
```

Now every push to `main` will automatically deploy!

---

## 🏗️ Option 2: Azure Static Web Apps + API

For a serverless approach with built-in CI/CD:

### Step 1: Create Static Web App

```bash
# Install SWA CLI
npm install -g @azure/static-web-apps-cli

# Create SWA resource
az staticwebapp create \
  --name mentalhealthjournal-swa \
  --resource-group rg-mentalhealthjournal \
  --location eastus2 \
  --sku Standard
```

### Step 2: Configure for .NET API

Create `staticwebapp.config.json` in repository root:

```json
{
  "routes": [
    {
      "route": "/api/*",
      "allowedRoles": ["anonymous"]
    }
  ],
  "navigationFallback": {
    "rewrite": "/index.html"
  },
  "platform": {
    "apiRuntime": "dotnet:8.0"
  }
}
```

### Step 3: Deploy via GitHub

1. Connect your GitHub repository in Azure Portal
2. Azure will auto-generate GitHub Actions workflow
3. Push to repository to trigger deployment

---

## 📊 Monitor Your App

### Application Insights

```bash
# Create Application Insights
az monitor app-insights component create \
  --app mentalhealthjournal-insights \
  --location eastus \
  --resource-group rg-mentalhealthjournal \
  --application-type web

# Get instrumentation key
INSTRUMENTATION_KEY=$(az monitor app-insights component show \
  --app mentalhealthjournal-insights \
  --resource-group rg-mentalhealthjournal \
  --query instrumentationKey -o tsv)

# Add to app settings
az webapp config appsettings set \
  --name mentalhealthjournal-app \
  --resource-group rg-mentalhealthjournal \
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=$INSTRUMENTATION_KEY"
```

### View Logs

```bash
# Stream logs
az webapp log tail \
  --name mentalhealthjournal-app \
  --resource-group rg-mentalhealthjournal

# Enable logging
az webapp log config \
  --name mentalhealthjournal-app \
  --resource-group rg-mentalhealthjournal \
  --application-logging filesystem \
  --level information
```

---

## 💰 Cost Optimization

### Development/Testing
- **App Service**: B1 (Basic) ~$13/month
- **Cosmos DB**: Serverless mode (pay per request)
- **Cognitive Services**: Free tier (5K transactions/month)
- **Azure OpenAI**: Pay per token
- **Blob Storage**: $0.18/GB/month

### Production
- **App Service**: S1 (Standard) ~$70/month for better performance
- Enable autoscaling based on load
- Consider reserved instances for 30% discount

---

## 🔧 Troubleshooting

### Issue: 500 Internal Server Error

**Check logs:**
```bash
az webapp log tail --name mentalhealthjournal-app --resource-group rg-mentalhealthjournal
```

**Common causes:**
- Missing environment variables
- Azure service credentials not configured
- Cosmos DB connection issues

### Issue: Frontend Not Loading

**Verify wwwroot:**
```bash
# Ensure frontend was built and copied
ls -la MentalHealthJournal.Server/wwwroot
```

Should contain `index.html`, `assets/` folder

### Issue: API Calls Failing

**Check CORS (if needed):**
Add to `Program.cs` if using separate domains:
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

---

## ✅ Deployment Checklist

Before deploying to production:

- [ ] Frontend built successfully (`npm run build`)
- [ ] Frontend copied to `wwwroot/`
- [ ] All Azure service credentials configured
- [ ] Environment set to "Production"
- [ ] HTTPS enforced
- [ ] Application Insights enabled
- [ ] Managed Identity configured (recommended)
- [ ] Cosmos DB database and containers created
- [ ] Blob Storage container created
- [ ] Azure OpenAI deployment exists
- [ ] Cognitive Services resource provisioned
- [ ] Secrets stored in Azure Key Vault or App Settings (not in code)
- [ ] GitHub Actions workflow tested (if using CI/CD)

---

## 🎉 Quick Start Script

Save this as `deploy-to-azure.sh`:

```bash
#!/bin/bash

# Configuration
RESOURCE_GROUP="rg-mentalhealthjournal"
LOCATION="eastus"
APP_NAME="mentalhealthjournal-app"
PLAN_NAME="plan-mentalhealthjournal"

# Login and set subscription
az login
az account set --subscription "YOUR_SUBSCRIPTION_NAME"

# Create resources
az group create --name $RESOURCE_GROUP --location $LOCATION
az appservice plan create --name $PLAN_NAME --resource-group $RESOURCE_GROUP --sku B1 --is-linux
az webapp create --name $APP_NAME --resource-group $RESOURCE_GROUP --plan $PLAN_NAME --runtime "DOTNET:8.0"

# Build and deploy
cd mentalhealthjournal.client
npm install && npm run build
mkdir -p ../MentalHealthJournal.Server/wwwroot
cp -r dist/* ../MentalHealthJournal.Server/wwwroot/

cd ../MentalHealthJournal.Server
dotnet publish -c Release -o ./publish
cd publish && zip -r ../deploy.zip . && cd ..

az webapp deployment source config-zip --name $APP_NAME --resource-group $RESOURCE_GROUP --src deploy.zip

echo "Deployment complete! Visit: https://$APP_NAME.azurewebsites.net"
```

Run: `chmod +x deploy-to-azure.sh && ./deploy-to-azure.sh`

---

## 📚 Next Steps After Deployment

1. **Custom Domain**: Add your own domain name
2. **SSL Certificate**: Configure custom SSL (free with App Service)
3. **Scaling**: Enable autoscaling based on CPU/memory
4. **Backup**: Configure automated backups
5. **Authentication**: Add Azure AD B2C for user login
6. **CDN**: Add Azure CDN for better global performance

---

**Your Mental Health Journal is ready for Azure! 🚀**

Choose Option 1 (App Service) for the quickest deployment.
