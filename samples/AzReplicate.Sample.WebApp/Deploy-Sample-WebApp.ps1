param
(
    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $SubscriptionId,

    # eastasia, southeastasia, centralus, eastus, eastus2, westus, northcentralus, southcentralus, northeurope, westeurope, japanwest, japaneast, brazilsouth, australiaeast, australiasoutheast
    # southindia, centralindia, westindia, canadacentral, canadaeast, uksouth, ukwest, westcentralus, westus2, koreacentral, koreasouth, francecentral, francesouth, australiacentral, 
    # australiacentral2, uaecentral, uaenorth, southafricanorth, southafricawest, switzerlandnorth, switzerlandwest, germanynorth, germanywestcentral, norwaywest, norwayeast
    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $Region, 
  
    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $ResourceGroup,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $DBServerName,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $DBName,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $DBUserName,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $DBUserPwd,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $AppServicePlan,
 
    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $WebAppName,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $AppDirectory
)

$Env:AZURE_CORE_ONLY_SHOW_ERRORS = $True
$Env:AZURE_CORE_OUTPUT = "tsv"

# Set defaults in case not provided

# Authenticate to Azure and target the appropriate subscription
az login

# Target the appropriate subscription
az account set `
    --subscription $SubscriptionId

# Set Region and ResourceGroup as script default
az configure `
    --defaults `
        location=$Region `
        group=$ResourceGroup 

# Create Resource Group
az group create `
    --name $ResourceGroup

# Create a SQL Database logical server
az sql server create --name $DBServerName `
    --resource-group $ResourceGroup `
    --location $Region `
    --admin-user $DBUserName `
    --admin-password $DBUserPwd

# Configure a server firewall rule to allow other Azure services to access our DB
az sql server firewall-rule create `
    --resource-group $ResourceGroup `
    --server $DBServerName `
    --name AllowAzureIps `
    --start-ip-address 0.0.0.0 `
    --end-ip-address 0.0.0.0

# Create a database
az sql db create `
    --resource-group $ResourceGroup `
    --server $DBServerName `
    --name $DBName `
    --service-objective S0

# Create connection string
$DBConnectionString = `
az sql db show-connection-string `
    --client ado.net `
    --server $DBServerName `
    --name $DBName

# Swap in the real username and password for the placeholders that were returned
$DBConnectionString = $DBConnectionString.Replace("<username>", $DBUserName).Replace("<password>", $DBUserPwd)

# Create an App Service plan  
az appservice plan create `
    --name $AppServicePlan `
    --resource-group $ResourceGroup `
    --sku FREE

# Create a web app
az webapp create `
    --resource-group $ResourceGroup `
    --plan $AppServicePlan `
    --name $WebAppName

# Configure connection string
az webapp config connection-string set `
    --resource-group $ResourceGroup `
    --name $WebAppName `
    --settings MyDbConnection=$DBConnectionString `
    --connection-string-type SQLAzure

# Publish the .NET App
dotnet publish $AppDirectory -c Release

# Compress the build output
Compress-Archive -Path "$AppDirectory\bin\Release\netcoreapp3.1\publish\*" -DestinationPath "$AppDirectory\bin\Release\netcoreapp3.1\AzReplicate.Sample.WebApp.zip" -Force

# publish it to Azure App Service
az webapp deployment source config-zip `
    --resource-group $ResourceGroup `
    --name $WebAppName `
    --src "$AppDirectory\bin\Release\netcoreapp3.1\AzReplicate.Sample.WebApp.zip"