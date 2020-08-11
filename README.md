# AzReplicate <!-- omit in toc -->

AzReplicate is a sample application designed to help Azure Storage customers preform very large, multi-petabyte data migrations to Azure Blob Storage. The sample was designed with a few key design objectives:

- The system shall scale to many multi-petabyte migrations.
- The system shall allow for the source file location to be internet addressable store. This could be Azure Storage, another cloud storage provider, CDN, web server, etc.
- The system shall be able to ingest the list of files needed to be transferred from any source including a database.
- The system shall notify the source system after a file is copied.
- The system shall provide scalable logging to allow for monitoring of the job and to provide an audit trail of the migration.

## Contents <!-- omit in toc -->

- [Problem Statement](#problem-statement)
  - [AzCopy](#azcopy)
  - [Azure Data Factory](#azure-data-factory)
- [Overview of AzReplicate and how it works](#overview-of-azreplicate-and-how-it-works)
- [AzReplicate Components](#azreplicate-components)
- [AzReplicate Implementation](#azreplicate-implementation)
- [Deploy AzReplicate](#deploy-azreplicate)
- [Monitoring AzReplicate](#monitoring-azreplicate)
- [Contributing](#contributing)

## Problem Statement

Many customers need to perform large multi-petabyte scale migrations of data from outside sources into Azure Blob or shard data from a single Azure Storage Account into multiple.  While there are a number of tools that contribute to the overall migration landscape, each have limitations that required a different set of tooling for this particular problem.  Many of the existing tools have excellent features for performing pre/post processing in a traditional ETL/ELT scenario, but few offered the raw throughput for massive multi-petabyte scale migrations without need for transformation.

### AzCopy

[AzCopy](https://aka.ms/AzCopy) provides a great way to copy files from one system to another. However, it assumes you can iterate a source storage location to generate the list of files to migrate. In the course of many customer engagements we have found that the list of files to migrate is in many cases stored in a database or other source system. Moreover, we have found that in many cases customers would like to reorganize the data during the migration process. Finally, in these cases, customers need to update the source system with the new location of each file after it has been migrated.

### Azure Data Factory

[Azure Data Factory](https://docs.microsoft.com/azure/data-factory/introduction) provides a very robust extract-transform-load (ETL), extract-load-transform (ELT), and data integration platform. However, migrations with ADF require all the data to flow through the worker nodes preforming the data migration. This can limit the scale of the transfer as these worker nodes can result in a bottleneck.

## Overview of AzReplicate and how it works

![AzReplicate Components](/docs/Components_300x436.png "AzReplicate Components")

<b>Sourcerer</b> - AzReplicate is a modular application that can copy any set of URL objects from a source URL to a destination blob inside an Azure Storage account.  The application requires a custom sourcerer which has the capability of queueing messages with source and destination endpoints for each object to be migrated.  

<b>Replicator Engine</b> - The AzReplicate Replicator Engine module reads each JSON message from the ToDo queue individually and issues a [Copy Blob From URL](https://docs.microsoft.com/rest/api/storageservices/copy-blob-from-url) or a [Put Block From URL](https://docs.microsoft.com/rest/api/storageservices/put-block-from-url) command to Blob Storage indicating the source and destination for each object to copy.  Using this technique, objects are never downloaded to the instance running this module which increases performance by reading and writing all objects directly into Blob Storage.  The design and use of containers allows for users of AzReplicate to scale out multiple instances or containers and fine tune the containers for optimal memory, threads and cores.  It is recommended to test and tune AzReplicate for a specific use case as needed by each migration scenario.  As each file is moved, the AzReplicate module will publish a JSON message into the Done Queue while unsuccessful copies will place a JSON message into the Dead Letter Queue for further analysis by the developers and admins running the tool.

<b>Completer</b> - Once objects are successfully written into Blob Storage a message is placed into a Done Queue for further processing.  This allows you to write code that can update the source system with the new location of the file. 

> Please refer to the AzReplicate [sample documentation](./docs/samples.md) that includes examples of Sourcerer, Replicator and Completer modules.

## AzReplicate Components

- Source: Any HTTP/HTTPS source that supports anonymous or SAS requests. This could be Azure Storage, another cloud storage provider, CDN, Web Server, etc.
  - The source system must support range lookups into the source objects.
- Destination: Any Azure Storage Account
- Sourcerer Module: An application that enqueues information about each object that need to be replicated. This application can read from a source system (database, etc), a CSV file, or even enumerate the files in a cloud storage account. Each message it enqueues contains at a minimum the source and target endpoints for one object that needs to be moved.
- AzReplicate Replicator Engine Module: Reads the ToDo queue and tells Azure Storage to copy the file from the source to the destination. If the object less than 256 MB it is copied using the [Copy Blob From URL](https://docs.microsoft.com/rest/api/storageservices/copy-blob-from-url) API, otherwise the object is copied in blocks using the [Put Block From URL](https://docs.microsoft.com/rest/api/storageservices/put-block-from-url) API.
  - The data is copied directly from the source to the destination, it is never downloaded onto the instance running the Replicator application.
  - If the replicator can successfully copy the data a message is placed into the Done Queue providing notification that the object has been moved.
  - If the replicator CANNOT copy the data a message is placed into the Dead Letter Queue providing notification that the object has NOT been moved.
- Completer Module: an application that reads the done queue and notifies the source system that a file has been moved.
- Object Logs: The replicator also records status for each source/destination pair to an Azure Storage Table, providing data for reporting on what objects have been moved even if an object requires multiple attempts to replicate.
- App Insights Logs: The AzReplicate Replicator Engine sends application level telemetry here. This provides an easy to query repository about application performance, such as quantity and speed of messages and data that is getting replicated, as well as details about any errors the process might be running into.

## AzReplicate Implementation

Each of the three modules of AzReplicate (Sourcerer, AzReplicate core, and Completer) are implemented as a .NET Core Application running in a Docker container. Each module is implemented as a separate container to allow each part of the application to be scaled appropriately. For example, typically you will run one instance of the Sourcerer to iterate over the objects in the source system to build the list of files that need to get moved. You will then likely run many instances of the AzReplicate core module to have Azure Storage copy the data in parallel, while monitoring the source system to ensure that you don't exceed the available bandwidth. Then you can choose when to start and stop the completer to slowly shift the application from reading the files from the new location.

When we use AzReplicate we deploy the containers using [Azure Container Instances](https://docs.microsoft.com/azure/container-instances/container-instances-overview). This allows us to dynamically provision the infrastructure needed to run the containers without the need for a large cluster and only pay for when our modules are running. However, you can deploy these containers to any infrastructure that can run a Docker Container like Kubernetes running in a managed environment like [Azure Kubernetes Service](https://docs.microsoft.com/azure/aks/intro-kubernetes) or on your own virtual machines).

> **Note:** To reduce latency and improve the performance of the job we recommend running the AzReplicate core module and deploy all the queues in the same Azure Region as the destination storage account.

## Deploy AzReplicate

[See instructions here](./docs/deploy.md)

## Monitoring AzReplicate

[See instructions here](./docs/monitor.md)


## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
