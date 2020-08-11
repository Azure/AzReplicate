# AzReplicate - Monitoring <!-- omit in toc -->


## Contents <!-- omit in toc -->
- [Replication failures](#replication-failures)
- [Replication successes and performance](#replication-successes-and-performance)
- [Replication worker performance](#replication-worker-performance)
- [Performance and statistics](#performance-and-statistics)

Application Insights is used within AzReplicate to capture and report telemetry about the running instance(s) of AzReplicate. The telemetry collected can be queried through the Application Insights service. The following sample queries surface the types of telemetry collected and can be used to troubleshoot replication errors between your source and destination.

## Replication failures

The following query can be used to report exceptions within AzReplicate and group them by type.

```sql
exceptions
| extend Type=iif(outerMessage startswith "Response status code does not indicate success: BadRequest", "Bad Request", outerMessage)
| extend Type=iif(Type startswith "Response status code does not indicate success: InternalServerError (Internal Server Error)", "Internal Server Error", Type)
| extend Type=iif(Type startswith "Response status code does not indicate success: Conflict (Conflict)", "Conflict", Type)
| extend Type=iif(Type startswith "Response status code does not indicate success: ServiceUnavailable", "Service Unavailable", Type)
| extend Type=iif(Type startswith "Response status code does not indicate success: Forbidden", "Forbidden", Type)
| extend Type=iif(Type startswith "Http transport failure", "Http Failure", Type)
| extend Type=iif(Type startswith "Response status code does not indicate success: BadGateway", "Bad Gateway", Type)
| summarize count() by Type
| render piechart
```

```sql
exceptions
| where outerMessage  == "Nullable object must have a value."
| project customDimensions._Source
```

```sql
exceptions
| where outerMessage startswith "Response status code does not indicate success: BadRequest"
| limit 100
```

```sql
exceptions
| where outerMessage startswith "Response status code does not indicate success: ServiceUnavailable"
| order by timestamp desc
| limit 100
```

To view a count of replication failures you can query for `type` or `outerType` where the value is `AzReplicate.Core.Exceptions.ReplicationFailedException` in the `exceptions` table.

```sql
exceptions
| where type == "AzReplicate.Core.Exceptions.ReplicationFailedException" or outerType == "AzReplicate.Core.Exceptions.ReplicationFailedException"
| count
```

## Replication successes and performance

The following query can be used to report successes. Note the use of constraining the result set for successes after a given time (*.e.g.* `timestamp > todatetime('2020-05-15 10:00:00')`)

> **Note:** All times are in UTC.

```sql
dependencies
| where name startswith "Replication Success"
| where timestamp > todatetime('2020-05-15 10:00:00')
| summarize ReplicatedSources=sum(tolong(customMeasurements._ReplicatedSources))
```

It is also possible to report on the amount of data transferred. For example:

```sql
dependencies
| where name startswith "Replication Success"
| where timestamp > todatetime('2020-05-15 10:00:00')
| summarize ReplicatedTiBs=sum(tolong(customMeasurements._ReplicatedBytes)) / pow(1024,4)
```

To determine valid dates within your existing data set you can quickly find the oldest entry with the following:

```sql
dependencies
| summarize min(timestamp)
```

It is also possible to query with greater granularity and report on the number of successes per hour, which can be beneficial for long running operations to understand ongoing performance and throughput.

```sql
dependencies
| where name startswith "Replication Success"
| where timestamp > todatetime('2020-05-01 00:00:00')
| order by timestamp desc
| limit 10
```

```sql
dependencies
| where name startswith "Replication Success"
| where timestamp > todatetime('2020-05-01 00:00:00')
| summarize sum(tolong(customMeasurements._ReplicatedSources)) by bin(timestamp, 1h)
| order by bin(timestamp, 1h)
| render timechart  
```

```sql
dependencies
| where name startswith "Replication Success"
| where timestamp > todatetime('2020-05-01 00:00:00')
| summarize
    ReplicatedSources=sum(tolong(customMeasurements._ReplicatedSources)),
    ReplicatedGiBs=sum(tolong(customMeasurements._ReplicatedBytes)) / pow(1024, 3),
    ReplicatedTiBs=sum(tolong(customMeasurements._ReplicatedBytes)) / pow(1024, 4),
    ReplicationsPerSecond=sum(tolong(customMeasurements._ReplicatedSources)) / 3600,
    ThroughputInGBps=((sum(tolong(customMeasurements._ReplicatedBytes))) / 3600) / pow(1024, 3),
    ThroughputInGbps=((sum(tolong(customMeasurements._ReplicatedBytes))) / 3600) / pow(1024, 3) * 8,
    AverageObjectSizeInMB=sum(tolong(customMeasurements._ReplicatedBytes)) / pow(1024, 2) / sum(tolong(customMeasurements._ReplicatedSources)),
    LastUpdatedOn=max(timestamp)
    by bin(timestamp, 1h)
| order by bin(timestamp, 1h)
```

```sql
dependencies
| where name startswith "Replication Success"
| summarize
    ReplicationsPerSecond=sum(tolong(customMeasurements._ReplicatedSources)) / 3600
    by bin(timestamp, 1h)
| order by bin(timestamp, 1h)
| render timechart
```

```sql
dependencies
| where name startswith "Replication Success"
| summarize
    ReplicatedGiBs=sum(tolong(customMeasurements._ReplicatedBytes)) / pow(1024, 3)
    by bin(timestamp, 1h)
| order by bin(timestamp, 1h)
| render timechart
```

```sql
dependencies
| where name startswith "Replication Success"
| summarize
    ReplicatedSources=sum(tolong(customMeasurements._ReplicatedSources))
    by bin(timestamp, 1h)
| order by bin(timestamp, 1h)
| render timechart
```

```sql
dependencies
| where name startswith "Replication Success"
| summarize
    ThroughputInGBps=((sum(tolong(customMeasurements._ReplicatedBytes))) / 3600) / pow(1024, 3),
    ThroughputInGbps=((sum(tolong(customMeasurements._ReplicatedBytes))) / 3600) / pow(1024, 3) * 8
    by bin(timestamp, 1h)
| order by bin(timestamp, 1h)
| render timechart
```

```sql
dependencies
| where name startswith "Replication Success"
| where timestamp > todatetime('2020-05-22 23:00:00') and timestamp < todatetime('2020-05-23 00:00:00')
| summarize ReplicatedSources=sum(tolong(customMeasurements._ReplicatedSources)) by bin(timestamp, 1m)
| order by timestamp
```

## Replication worker performance

The following queries can be used to understand how many replications are occurring across each AzReplicate worker (container instance) that is deployed.

```sql
dependencies
| summarize count(), max(timestamp) by tostring(customDimensions._CorrelationId)
```

```sql
traces
| where message startswith "Worker Starting" or message startswith "Worker Ending"  
| summarize min(timestamp), max(timestamp) by tostring(customDimensions._CorrelationId)
```

If you would like to query a particular worker process after determining a `CorrelationId` using one of the above queries:

```sql
traces
| where customDimensions._CorrelationId == "00000000-0000-0000-0000-000000000000"
| order by timestamp
```

## Performance and statistics

```sql
customMetrics
| where name startswith "_" and timestamp > todatetime('2020-05-15 01:00:00')
| summarize
    Value=sum(value),
    StartedAt=format_datetime(min(timestamp), 'yyyy-MM-dd hh:mm:ss'),
    LastTraceAt=format_datetime(max(timestamp), 'yyyy-MM-dd hh:mm:ss') by name
| evaluate
    pivot(name, sum(Value))
| project
    StartedAt,
    LastTraceAt,
    ReplicatedSources=_ReplicatedSources,
    ReplicatedBytes=_ReplicatedBytes,
    ReplicatedGiBs=_ReplicatedBytes/pow(1024, 3),
    ReplicatedTiBs=_ReplicatedBytes/pow(1024, 4)
| summarize
    ReplicatedSources=sum(ReplicatedSources),
    ReplicatedBytes=sum(ReplicatedBytes),
    ReplicatedTiBs=sum(ReplicatedTiBs)
    by StartedAt, LastTraceAt
| extend
    ReplicatedIn=todatetime(LastTraceAt)-todatetime(StartedAt),
    ReplicatedInSeconds=datetime_diff('second', todatetime(LastTraceAt), todatetime(StartedAt))
| extend
    ThroughputInBytes=ReplicatedBytes/ReplicatedInSeconds,
    ReplicationsPerSecond=ReplicatedSources/ReplicatedInSeconds
| extend
    ThroughputInMB=ThroughputInBytes/pow(1024, 2),
    ThroughputInMbps=ThroughputInBytes/pow(1024, 2) * 8,
    ThroughputInGB=ThroughputInBytes/pow(1024, 3),
    ThroughputInGbps=ThroughputInBytes/pow(1024, 3) * 8
| project
    ReplicatedSources,
    ReplicatedBytes,
    ReplicatedTiBs,
    ReplicatedIn,
    ReplicatedInSeconds,
    ThroughputInMbps,
    ThroughputInGbps,
    ReplicationsPerSecond
```

Which returns an output similar to the following:

| ReplicatedSources | ReplicatedBytes | ReplicatedTiBs | ReplicatedIn | ReplicatedInSeconds | ThroughputInMbps | ThroughputInGbps | ReplicationsPerSecond |
| ----------------- | --------------- | -------------- | ------------ | ------------------- | ---------------- | ---------------- | --------------------- |
| 49                | 5947670         | 5.40937E-06    | 0:00:30      | 30                  | 1.512570699      | 0.00147712       | 1.633333333           |

Another view of general statistics, including the replication duration and estimated completion can be viewed with:

```sql
dependencies
| where name startswith "Replication Success"
| where tolong(customMeasurements._ReplicatedSources) > 0
| summarize
    ReplicationDuration=max(timestamp) - min(timestamp),
    ReplicationDurationInSeconds=datetime_diff("Second", max(timestamp), min(timestamp)),
    ReplicatedSources=sum(tolong(customMeasurements._ReplicatedSources)),
    ReplicatedTiBs=sum(tolong(customMeasurements._ReplicatedBytes)) / pow(1024, 4),
    RemainingSources=600000000-sum(tolong(customMeasurements._ReplicatedSources)),
    RemainingTiBs=1700-sum(tolong(customMeasurements._ReplicatedBytes)) / pow(1024, 4),
    Progress=(sum(tolong(customMeasurements._ReplicatedBytes)) / pow(1024, 4)/1700)*100
| project
    ReplicationDuration,
    ReplicatedSources,
    ReplicatedTiBs,
    RemainingSources,
    RemainingTiBs,
    Progress,
    EstimatedToBeCompletedAt=datetime_add("Second", tolong((RemainingTiBs / (ReplicatedTiBs / ReplicationDurationInSeconds))), now())
```

Which returns an output similar to the following:

| ReplicationDuration | ReplicatedSources | ReplicatedTiBs | RemainingSources | RemainingTiBs | Progress | EstimatedToBeCompletedAt [UTC] |
| ------------------- | ----------------- | -------------- | ---------------- | ------------- | -------- | ------------------------------ |
| 00:00:29.9901423    | 49                | 0              | 599,999,951      | 1,700         | 0        | 4/2/2319, 10:32:13.162 PM      |
