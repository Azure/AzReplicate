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
    [String] $StorageAccount,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [AllowEmptyString()]
    [String] $ReplicationQueueName,

    [Parameter(Mandatory = $False, valueFromPipeline=$True)]
    [int] $MaximumNumberOfConcurrentReplications,

    [Parameter(Mandatory = $False, valueFromPipeline=$True)]
    [int] $ReplicationBlockSizeInBytes,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $ContainerRegistry,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [String] $ApplicationInsights,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [int] $InstanceCount,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [int] $AmountOfCoresPerContainer,

    [Parameter(Mandatory = $True, valueFromPipeline=$True)]
    [int] $AmountOfMemoryPerContainer
)

$Env:AZURE_CORE_ONLY_SHOW_ERRORS = $True
$Env:AZURE_CORE_OUTPUT = "tsv"

# Set defaults in case not provided
if ($ReplicationQueueName -eq $null -or $ReplicationQueueName -eq "") { $ReplicationQueueName = "replication" }
if ($AmountOfCoresPerContainer -eq 0) { $AmountOfCoresPerContainer = 1 }
if ($AmountOfMemoryPerContainer -eq 0) { $AmountOfMemoryPerContainer = 1 }
if ($MaximumNumberOfConcurrentReplications -eq 0) { $MaximumNumberOfConcurrentReplications = 32 }
if ($ReplicationBlockSizeInBytes -eq 0) { $ReplicationBlockSizeInBytes = 32 * 1024 * 1024 }

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
$StorageAccountConnectionString = `
az storage account show-connection-string `
    --name $StorageAccount

# Request authentication information from application insights
$ApplicationInsightsInstrumentationKey = `
az monitor app-insights component show `
    --app $ApplicationInsights `
    --query instrumentationKey

$RunningContainers = `
@(az container list `
    --query [*].name `
    | Where-Object { $_ -Like "azreplicate-instance-*" } `
    | Sort-Object -Descending { $_ })

if ($RunningContainers.Length -eq $InstanceCount) 
{
    Write-Output "Not starting new instances, already running $($InstanceCount) instance(s)"
    Write-Output $RunningContainers
}
else 
{
    if ($RunningContainers.Length -lt $InstanceCount) 
    {
        # Deploy Container Instance(s)
        Write-Output "Starting $($InstanceCount - $RunningContainers.Length) instance(s)"

        for($InstanceCounter = $RunningContainers.Count; $InstanceCounter -lt $InstanceCount; $InstanceCounter++)
        {
            $ContainerName = "azreplicate-instance-$(($InstanceCounter + 1).ToString("000"))"

            Write-Output "$($ContainerName)"

            az container create `
                --name $ContainerName `
                --cpu $AmountOfCoresPerContainer `
                --memory $AmountOfMemoryPerContainer `
                --registry-login-server $ContainerRegistryLoginServer `
                --registry-username $ContainerRegistryUsername `
                --registry-password $ContainerRegistryPassword `
                --image "$($ContainerRegistryLoginServer)/azreplicate:latest" `
                --environment-variables `
                    AppSettings__QueueConnectionString=$StorageAccountConnectionString `
                    AppSettings__QueueName=$ReplicationQueueName `
                    AppSettings__TableConnectionString=$StorageAccountConnectionString `
                    AppSettings__MaximumNumberOfConcurrentMessageHandlers=$MaximumNumberOfConcurrentReplications `
                    AppSettings__ReplicationBlockSizeInBytes=$ReplicationBlockSizeInBytes `
                    ApplicationInsights__InstrumentationKey=$ApplicationInsightsInstrumentationKey `
                --no-wait
        }
    }
    else 
    {
        # Terminate Container Instance(s)
        Write-Output "Stopping $($RunningContainers.Length - $InstanceCount) instance(s)"

        for($InstanceCounter = 0; $InstanceCounter -lt ($RunningContainers.Length - $InstanceCount); $InstanceCounter++) 
        {
            Write-Output "$($RunningContainers[$InstanceCounter])"

            az container delete `
                --name $RunningContainers[$InstanceCounter] `
                --yes
        }
    }
}

