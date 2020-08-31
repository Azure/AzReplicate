# AzReplicate - Samples <!-- omit in toc -->

These instructions outline the steps to deploy sample sourcerer/completer modules for AzReplicate. To use these samples you will also need to use the AzReplicate Replicator Engine Module. We recommend you start by reading the documentation [here](/README.md) if you have not done so already.

We provide the following samples:
 - Web Application Sample - This sample has a website with a MS SQL Database. The web app gets a list of image URLs to display from the database. Before the migration the images URLs are pointing to AWS S3. The migration copies the images to Azure and then updates the database with the new Azure Storage URLs. 
 - Azure Blob Sample - This sample copies the data in one Azure Storage account to 1 or more Azure Storage accounts using a round robin distribution. Partitioning/sharding data across different storage accounts allows for increase total capacity/thruput. 

## Contents <!-- omit in toc -->
- [Web App Sample](#web-app-sample)
  - [Deploy Web App](#deploy-web-app)
  - [Build and Publish, & Deploy the Web App Sourcerer](#build-and-publish--deploy-the-web-app-sourcerer)
  - [Deploy the Web App Completer](#deploy-the-web-app-completer)
- [Shard Blob Storage Sample](#shard-blob-storage-sample)
  - [Build and Publish, & Deploy the Blob Sourcerer](#build-and-publish--deploy-the-blob-sourcerer)

## Web App Sample

This sample has a website with a MS SQL Database. The web app gets a list of image URLs to display from the database. Before the migration the images URLs are pointing to AWS S3. The migration copies the images to Azure and then updates the database with the new Azure Storage URLs. 

### Deploy Web App
The first part of this sample is a simple .NET website, hosted on Azure App Services, with a SQL server that stores a list of image URLs pointing to images that live on S3. To deploy the sample execute the [Deploy-WebAppSample.ps1](./samples/AzReplicate.Sample.WebApp/Deploy-Sample-WebApp.ps1) script.

```PowerShell
./samples/AzReplicate.Sample.WebApp/Deploy-Sample-WebApp.ps1 `
    -SubscriptionId $subscriptionId `
    -Region $Region `
    -ResourceGroup $resourceGroup `
    -DBServerName $dbServer <# the name of the SQL server you want to create, be sure to pick something unique and that follows naming rules #> `
    -DBName $dbname <# the name of the database on the SQL Server to create #> `
    -DBUserName $dbuser <# the username of the sql server admin #> `
    -DBUserPwd $dbpwd <# the password of the sql server admin #> `
    -AppServicePlan $appsvcplan <# the app service plan name to create #> `
    -WebAppName $webapp <# the web app to create, be sure to pick something unique and that follows naming rules #> `
    -AppDirectory "./samples/AzReplicate.Sample.WebApp/" <# the relative path to where the sample .NET code for the website lives on your computer #>
```

After you execute this script you can open a browser to the sample application by going to https://yourwebappname.azurewebsites.net. You will not see any images till you press the "Reset" link at the top of the page. This will go out and get 50 sample records, save them to the database and refresh the page.

You should now see the images rotating through, as well as the URL to where each image is currently getting loaded from.

### Build and Publish, & Deploy the Web App Sourcerer

The [Deploy-Sample-WebApp-Sourcerer](./samples/AzReplicate.Sample.WebAppSourcerer/Deploy-Sample-WebApp-Sourcerer.ps1) script. Will build the container, publish it to the registry and deploy it to Azure Container Instances.

```PowerShell
./samples/AzReplicate.Sample.WebAppSourcerer/Deploy-Sample-WebApp-Sourcerer.ps1 `
    -SubscriptionId $subscriptionId `
    -Region $Region `
    -ResourceGroup $resourceGroup `
    -SourcePath "./samples/" `
    -TargetStorageAccountName $storageAccount <# Target storage account for the queue and the objects #> `
    -TargetStorageAccountContainerName data <# Name of the container in the target storage account #> `
    -ReplicationQueueName replication <# Name of the queue in the target storage account #> `
    -DBServerName $dbserver <# the name of the SQL server you want to use #> `
    -DBName $dbname <# the name of the database on the SQL Server to use #> `
    -DBUserName $dbuser <# the username of the sql server admin #> `
    -DBUserPwd $dbpwd <# the password of the sql server admin #> `
    -ContainerRegistry $ContainerRegistry <# the ACR that the container was published to #> `
    -AmountOfCoresPerContainer 1 <# the number of cores for the running container #> `
    -AmountOfMemoryPerContainer 1 <# the GB of memory for the running container #>
```

### Deploy the Web App Completer

The [Deploy-Sample-WebApp-Completer](./samples/AzReplicate.Sample.WebAppCompleter/Deploy-Sample-WebApp-Completer.ps1) script. Will build the container, publish it to the registry and deploy it to Azure Container Instances.

```PowerShell
./samples/AzReplicate.Sample.WebAppCompleter/Deploy-Sample-WebApp-Completer.ps1 `
    -SubscriptionId $subscriptionId `
    -Region $Region `
    -ResourceGroup $resourceGroup `
    -SourcePath "./samples/" `
    -TargetStorageAccountName $storageAccount <# Target storage account for the queue and the objects #> `
    -CompleterQueueName completion <# Name of the queue in the target storage account #> `
    -DBServerName $dbserver <# the name of the SQL server you want to use #> `
    -DBName $dbname <# the name of the database on the SQL Server to use #> `
    -DBUserName $dbuser <# the username of the sql server admin #> `
    -DBUserPwd $dbpwd <# the password of the sql server admin #> `
    -ContainerRegistry $ContainerRegistry <# the ACR that the container was published to #> `
    -AmountOfCoresPerContainer 1 <# the number of cores for the running container #> `
    -AmountOfMemoryPerContainer 1 <# the GB of memory for the running container #>
```


## Shard Blob Storage Sample

This sample copies the data in one Azure Storage account to 1 or more Azure Storage accounts using a round robin distribution. Partitioning/sharding data across different storage accounts allows for increase total capacity/thruput. 

### Build and Publish, & Deploy the Blob Sourcerer

```PowerShell
./samples/AzReplicate.Sample.AzBlobSourcerer/Deploy-Sample-AzBlob-Sourcerer.ps1 `
    -SubscriptionId $subscriptionId `
    -Region $Region `
    -ResourceGroup $resourceGroup `
    -SourcePath "./src/AzReplicate.Sample/" `
    -SourceStorageAccountResourceGroup "sourcerg" <# source storage account resource group for the objects #> `
    -SourceStorageAccountName $storageAccount <# source storage account for the objects #> `
    -DestinationStorageAccountNames "dest1,dest2" <# Name of the destination storage accounts comma separated #> `
    -ReplicationQueueAccountName "dest1" <# Name of the queue to use for replication #> `
    -ReplicationQueueName replication <# Name of the queue to use for replication #> `
    -ContainerRegistry $ContainerRegistry <# the ACR that the container was published to #> `
    -AmountOfCoresPerContainer 1 <# the number of cores for the running container #> `
    -AmountOfMemoryPerContainer 1 <# the GB of memory for the running container #>
```