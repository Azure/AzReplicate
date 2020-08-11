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
    [String] $SourcePath,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $TargetStorageAccountName,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [AllowEmptyString()]
    [String] $TargetStorageAccountContainerName,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [AllowEmptyString()]
    [String] $ReplicationQueueName,

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
if ($ReplicationQueueName -eq $null -or $ReplicationQueueName -eq "") { $ReplicationQueueName = "replication" }
if ($AmountOfCoresPerContainer -eq 0) { $AmountOfCoresPerContainer = 2 }
if ($AmountOfMemoryPerContainer -eq 0) { $AmountOfMemoryPerContainer = 4 }

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

    
# Create the storage account (if needed)
$TargetStorageAccountAvailable = `
az storage account check-name `
    --name $TargetStorageAccountName `
    --query "nameAvailable"

if ($TargetStorageAccountAvailable)
{
    ## nobody has this account, create it 
    az storage account create `
        --name $TargetStorageAccountName `
        --sku Standard_LRS
}
else {    

    $TargetStorageAccountsInResourceGroup = `
    az storage account list `
        -g $ResourceGroup `
        --query "[?name=='$TargetStorageAccountName'] | length(@)"

        if ($TargetStorageAccountsInResourceGroup -ne 1)
        {
            ## somebody has this account and it is not me
            Write-Error -Message "The storage account name is already taken by someone else."
            exit
        }
        else {
            Write-Warning -Message "Using existing target storage account, data might be overwritten."
        }

}

# Request authentication information from storage account
$TargetStorageAccountConnectionString = `
az storage account show-connection-string `
    --name $TargetStorageAccountName

$DestPathRoot = `
az storage account show `
    --name $TargetStorageAccountName `
    --query "primaryEndpoints.blob"

# Create the container if needed
$TargetStorageAccountContainerExists = `
az storage container exists `
    --name $TargetStorageAccountContainerName `
    --connection-string $TargetStorageAccountConnectionString `
    --query "exists"

if ($TargetStorageAccountContainerExists -eq "false")
{
    az storage container create `
        --public-access blob `
        --name $TargetStorageAccountContainerName `
        --connection-string $TargetStorageAccountConnectionString `
}

# Create/update the access policy
## NOTE: deleting this policy will invalidate all the child SAS keys
##       causing any queue messages with them to fail
az storage container policy create `
    --container-name $TargetStorageAccountContainerName `
    --connection-string $TargetStorageAccountConnectionString `
    --name "AzReplicatePolicy" `
    --expiry (Get-Date).AddMonths(1).ToString("yyyy-MM-d") `
    --permissions acw

#Create SAS Key
$TargetStorageAccountSAS = `
az storage container generate-sas `
   --name $TargetStorageAccountContainerName `
   --connection-string $TargetStorageAccountConnectionString `
   --policy-name "AzReplicatePolicy"

# Escape the &'s in the SAS key or it will break setting the environment variable later
$TargetStorageAccountSAS = $TargetStorageAccountSAS.Replace("&", """&""")   

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
    --file "$($SourcePath)/AzReplicate.Sample.WebAppSourcerer/Dockerfile" `
    --image webappsourcerer:latest

# Create the container
az container create `
    --name "webappsourcerer" `
    --cpu $AmountOfCoresPerContainer `
    --memory $AmountOfMemoryPerContainer `
    --registry-login-server $ContainerRegistryLoginServer `
    --registry-username $ContainerRegistryUsername `
    --registry-password $ContainerRegistryPassword `
    --image "$($ContainerRegistryLoginServer)/webappsourcerer:latest" `
    --restart-policy Never `
    --environment-variables `
        DestinationPathRoot=$DestPathRoot `
        DestinationSAS="$TargetStorageAccountSAS" `
        ReplicationQueueName=$ReplicationQueueName `
        ConnectionStrings__MyDbConnection=$DBConnectionString `
        ConnectionStrings__MyStorageConnection=$TargetStorageAccountConnectionString