# AzReplicate - Deployment <!-- omit in toc -->

## Contents <!-- omit in toc -->
- [AzReplicate Prerequisites](#azreplicate-prerequisites)
- [Deploy the AzReplicate Infrastructure, Build the AzReplicate container, and publish it to ACR](#deploy-the-azreplicate-infrastructure-build-the-azreplicate-container-and-publish-it-to-acr)
- [Start/Stop instances of AzReplicate](#startstop-instances-of-azreplicate)

## AzReplicate Prerequisites

AzReplicate requires the following Azure services and role assignments to be deployed:

- Access to an Azure subscription with appropriate quotas
- Contributor (or equivalent) [RBAC](https://docs.microsoft.com/azure/role-based-access-control/overview) assignments at a subscription scope to create the following Azure services:
  - [Azure Storage Account](https://azure.microsoft.com/services/storage/)
    - [Blob Storage](https://azure.microsoft.com/services/storage/blobs/)
    - [Queue Storage](https://azure.microsoft.com/services/storage/queues/)
  - [Azure Container Registry](https://azure.microsoft.com/services/container-registry/)
  - [Azure Container Instances](https://azure.microsoft.com/services/container-instances/)
  - [Azure Application Insights](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)

AzReplicate is also dependent on a series of scripts to deploy its samples and the AzReplicate components. To deploy these, the following is required:

- [Azure CLI](https://docs.microsoft.com/cli/azure/what-is-azure-cli?view=azure-cli-latest)
- [PowerShell](https://docs.microsoft.com/powershell/scripting/overview?view=powershell-7)
  - 5.1+ on Windows
  - 6 on Linux/macOS

To deploy the [sample application](./samples.md), you will need the following in addition to any tooling noted above:

- [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)

## Deploy the AzReplicate Infrastructure, Build the AzReplicate container, and publish it to ACR

``` PowerShell
# Shared variables
$SubscriptionId="<Subscription Id>" # the GUID of your subscription
$Region="<Region Short Name>" # the Azure region you want to deploy to, pick one that has all the available services and preferably the same one as the destination storage accounts
$ResourceGroup="<Resource Group Name>" # the name of your resource group, unique in your subscription
$StorageAccount="<Storage Account Name>" # the name of the storage account you want to create for the queues and logs, it needs to be globally unique, just the name not the URL.
$ContainerRegistry="<Azure Container Registry Name>" # the name of the container registry you want to create, globally unique, just the name not the URL
$ApplicationInsights="<Application Insights Resource Name>" # the name of the Application Insights instance you want to create
$SourcePath="./src" <# the local path to the code on your computer #>

# Register Application Insights extension
az extension add `
    --name application-insights

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

# Create Storage Account
az storage account create `
    --name $StorageAccount `
    --access-tier Hot `
    --kind StorageV2 `
    --sku Standard_LRS `
    --https-only true

# Create Container Registry
az acr create `
    --name $ContainerRegistry `
    --admin-enabled true `
    --sku Standard

# Create Application Insights
az monitor app-insights component create `
    --app $ApplicationInsights

# Build AzReplicate Container and Publish it to ACR
az acr build $SourcePath `
    --registry $ContainerRegistry `
    --file "$($SourcePath)/AzReplicate/Dockerfile" `
    --image azreplicate:latest

```

## Start/Stop instances of AzReplicate

The [provided powershell script](Deploy-AzReplicate.ps1) will start/stop running instances of AzReplicate. If you provide a Instance Count higher than what you have running, it will create more of the specified size. If you provide an Instance Count lower than what you have running, it will shut down instances to bring you to your target instance count. This makes it easy to dial up/down the number of instances of AzReplicate you have running to optimize the amount of bandwidth you are using on the source/destination locations. 

> **NOTE** You will typically deploy AzReplicate after you have deployed your sourcerer and it has started pushing messages into the ToDo queue. If the ToDo queue is empty and you start instances of AzReplicate they will monitor the ToDo queue waiting for new messages to arrive. 

```powershell
./docs/Deploy-AzReplicate.ps1 `
  -SubscriptionId $SubscriptionId `
  -Region $Region `
  -ResourceGroup $ResourceGroup `
  -ContainerRegistry $ContainerRegistry `
  -ApplicationInsights $ApplicationInsights `
  -StorageAccount $StorageAccount `
  -ReplicationQueueName "replication" <# the name of the ToDo Queue #> `
  -MaximumNumberOfConcurrentReplications 32 <# number of parallel threads to use inside of each instance of AzReplicate, we typically use 32 #> `
  -ReplicationBlockSizeInBytes 33554432 <# we typically use 32MB, however choose a block size that makes sense for the size of the files you are replicating - too big and any transient errors will require longer to retry - too small and you will accrue more transaction charges #> `
  -InstanceCount 1 <# The number of instances of AzReplicate you want to run #> `
  -AmountOfCoresPerContainer 1 <# Number of CPUs that are required, we typically use 1 to keep costs low #> `
  -AmountOfMemoryPerContainer 1 <# Amount of Memory (in GB) that is required, we typically use 1 to keep costs low #>

```
