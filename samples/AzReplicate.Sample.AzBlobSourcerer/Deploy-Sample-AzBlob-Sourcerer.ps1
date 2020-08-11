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
    [String] $SourceStorageAccountResourceGroup,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $SourceStorageAccountName,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $DestinationStorageAccountNames,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $ReplicationQueueAccountName,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [AllowEmptyString()]
    [String] $ReplicationQueueName,

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

# Create the replication queue storage account (if needed)
$ReplicationQueueAccountNameAvailable = `
az storage account check-name `
    --name $ReplicationQueueAccountName `
    --query "nameAvailable"

if ($ReplicationQueueAccountNameAvailable)
{
    ## nobody has this account, create it 
    az storage account create `
        --name $ReplicationQueueAccountName `
        --sku Standard_LRS
}
else {    

    $ReplicationQueueAccountsInResourceGroup = `
    az storage account list `
        -g $ResourceGroup `
        --query "[?name=='$ReplicationQueueAccountName'] | length(@)"

        if ($ReplicationQueueAccountsInResourceGroup -ne 1)
        {
            ## somebody has this account and it is not me
            Write-Error -Message "The storage account name is already taken by someone else."
            exit
        }
        else {
            Write-Warning -Message "Using existing target storage account, data might be overwritten."
        }
}

# Request authentication information from queue storage account
$ReplicationQueueAccountConnectionString = `
az storage account show-connection-string `
    --name $ReplicationQueueAccountName

# Request authentication information from source storage account
$SourceStorageAccountConnectionString = `
az storage account show-connection-string `
    -g $SourceStorageAccountResourceGroup `
    --name $SourceStorageAccountName


$DestinationStorageAccountConnectionStrings = @()

# iterate over each destination storage account
foreach ($DestinationStorageAccountName in $DestinationStorageAccountNames.Split(","))
{
    # Create the replication queue storage account (if needed)
    $DestinationStorageAccountNameAvailable = `
    az storage account check-name `
        --name $DestinationStorageAccountName `
        --query "nameAvailable"

    if ($DestinationStorageAccountNameAvailable)
    {
        ## nobody has this account, create it 
        az storage account create `
            --name $DestinationStorageAccountName `
            --sku Standard_LRS
    }
    else {    

        $DestinationStorageAccountsInResourceGroup = `
        az storage account list `
            -g $ResourceGroup `
            --query "[?name=='$DestinationStorageAccountName'] | length(@)"

            if ($DestinationStorageAccountsInResourceGroup -ne 1)
            {
                ## somebody has this account and it is not me
                Write-Error -Message "The storage account name is already taken by someone else."
                exit
            }
            else {
                Write-Warning -Message "Using existing target storage account, data might be overwritten."
            }
    }

    # Request authentication information from queue storage account
    $DestinationStorageAccountConnectionStrings += `
    az storage account show-connection-string `
        --name $DestinationStorageAccountName    
}

# Build Container
az acr build $SourcePath `
    --registry $ContainerRegistry `
    --file "$($SourcePath)/AzReplicate.Sample.AzBlobSourcerer/Dockerfile" `
    --image azblobsourcerer:latest

# Prepare the environment variables for the container
## Put them into a hashtable
$envVars = @{
    'ReplicationQueueName'=$ReplicationQueueName;
    'ConnectionStrings__QueueStorageConnection'=$ReplicationQueueAccountConnectionString;
    'ConnectionStrings__SourceStorageConnection'=$SourceStorageAccountConnectionString
}

$destRow = 1;
foreach ($DestinationStorageAccountConnectionString in $DestinationStorageAccountConnectionStrings)
{
    $envVars += @{"ConnectionStrings__DestinationStorageConnection$destRow"=$DestinationStorageAccountConnectionString}
    $destRow++;
}

## Flaten the hashtable into a list of strings key=value
$envVars = $envVars.GetEnumerator() | ForEach-Object { $x = '"{0}"="{1}"' -f $_.key, $_.value; Write-Output $x }

# Create the container
az container create `
    --name "azblobsourcerer" `
    --cpu $AmountOfCoresPerContainer `
    --memory $AmountOfMemoryPerContainer `
    --registry-login-server $ContainerRegistryLoginServer `
    --registry-username $ContainerRegistryUsername `
    --registry-password $ContainerRegistryPassword `
    --image "$($ContainerRegistryLoginServer)/azblobsourcerer:latest" `
    --restart-policy Never `
    --environment-variables $envVars
