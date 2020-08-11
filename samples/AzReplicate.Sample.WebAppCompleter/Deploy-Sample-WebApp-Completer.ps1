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
    [AllowEmptyString()]
    [String] $SourcePath,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $TargetStorageAccountName,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [AllowEmptyString()]
    [String] $CompleterQueueName,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $DBServerName,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $DBName,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $DBUserName,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $DBUserPwd,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $ContainerRegistry,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [int] $AmountOfCoresPerContainer,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [int] $AmountOfMemoryPerContainer
)

$Env:AZURE_CORE_ONLY_SHOW_ERRORS = $True
$Env:AZURE_CORE_OUTPUT = "tsv"

# Set defaults in case not provided
if ($SourcePath -eq $null -or $SourcePath -eq "") { $SourcePath = "./sample/AzReplicate.Sample/" }
if ($CompleterQueueName -eq $null -or $CompleterQueueName -eq "") { $CompleterQueueName = "completion" }
if ($AmountOfCoresPerContainer -eq 0) { $AmountOfCoresPerContainer = 1 }
if ($AmountOfMemoryPerContainer -eq 0) { $AmountOfMemoryPerContainer = 1 }

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

# Request authentication information from container registry
$ContainerRegistryLoginServer = `
az acr show `
    --name $ContainerRegistry `
    --query loginServer

$ContainerRegistryUsername = `
az acr credential show `
    --name $ContainerRegistry `
    --query username

$ContainerRegistryPassword = `
az acr credential show `
    --name $ContainerRegistry `
    --query passwords[0].value

# Request authentication information from storage account
$TargetStorageAccountConnectionString = `
az storage account show-connection-string `
    --name $TargetStorageAccountName

# Create database connection string
$DBConnectionString = `
az sql db show-connection-string `
    --client ado.net `
    --server $DBServerName `
    --name $DBName

# Swap in the real username and password for the placeholders that were returned
$DBConnectionString = $DBConnectionString.Replace("<username>", $DBUserName).Replace("<password>", $DBUserPwd)

# Build Container
az acr build $SourcePath `
    --registry $ContainerRegistry `
    --file "$($SourcePath)/AzReplicate.Sample.WebAppCompleter/Dockerfile" `
    --image webappcompleter:latest

# Create the container
az container create `
    --name "webappcompleter" `
    --cpu $AmountOfCoresPerContainer `
    --memory $AmountOfMemoryPerContainer `
    --registry-login-server $ContainerRegistryLoginServer `
    --registry-username $ContainerRegistryUsername `
    --registry-password $ContainerRegistryPassword `
    --image "$($ContainerRegistryLoginServer)/webappcompleter:latest" `
    --restart-policy Never `
    --environment-variables `
        CompleterQueueName=$CompleterQueueName `
        ConnectionStrings__MyDbConnection=$DBConnectionString `
        ConnectionStrings__MyStorageConnection=$TargetStorageAccountConnectionString