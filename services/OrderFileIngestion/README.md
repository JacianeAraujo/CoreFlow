# Order File Ingestion

Serverless ingestion flow for financial reconciliation CSV files.

## Architecture

```
CSV upload
    │
    ▼
S3 bucket: order-file-upload-s3 ── ObjectCreated:* ──▶ Lambda: order-file-ingestion-lambda
                                                                  │
                                                                  ├─ Validate metadata (size, filename, provider)
                                                                  ├─ Read CSV headers, validate schema/required columns
                                                                  ├─ Count records
                                                                  ▼
                                                       SNS topic: order-file-reconciliation-batch
                                                                  │
                                                                  ├──▶ (future) SQS: reconciliation-worker
                                                                  └──▶ (future) SQS: audit-trail
                                                                              │
                                                                              ▼
                                                                       ECS Fargate workers
```

The Lambda is intentionally lightweight: it never does the heavy reconciliation
work itself. It validates, publishes a `ReconciliationBatchReadyEvent`, and
returns. ECS workers then fan out from SNS via SQS subscriptions to do the
heavy lifting asynchronously.

## Project layout

```
services/OrderFileIngestion/
├── OrderFileIngestion.sln
├── build.sh                              # builds the Lambda .zip used by Terraform
├── src/OrderFileIngestion/
│   ├── Function.cs                       # Lambda entry point (HandleAsync)
│   ├── Models/                           # OrderRecord, ReconciliationBatchEvent
│   ├── Options/IngestionOptions.cs       # Env-var-driven configuration
│   ├── Services/                         # CsvSchemaValidator, ReconciliationEventPublisher
│   ├── Validation/                       # FileMetadataValidator, ValidationException
│   └── aws-lambda-tools-defaults.json
└── tests/OrderFileIngestion.Tests/       # xUnit tests for validators
```

## CSV contract

The Lambda expects S3 keys shaped as:

```
<provider>/file-YYYY-MM-DD.csv
```

Required columns (case-insensitive, comma-delimited):

| Column        | Type    |
|---------------|---------|
| order_id      | string  |
| client_id     | string  |
| provider      | string  |
| order_type    | string  |
| asset_symbol  | string  |
| quantity      | number  |
| unit_price    | number  |
| order_date    | ISO date|

## Validation rules

| Rule                        | Failure kind            |
|-----------------------------|-------------------------|
| Empty file (0 bytes / no rows) | `EmptyFile`         |
| Key does not match pattern   | `InvalidFilename`     |
| Provider not in allow-list   | `InvalidProvider`     |
| Missing required columns     | `InvalidSchema`       |
| File size > `MAX_FILE_SIZE_BYTES` | `UnexpectedFileSize` |

Validation failures are logged at `WARN` and the invocation completes
successfully so S3 does not retry. Unexpected/transient errors are re-thrown
so Lambda's built-in retry + DLQ semantics take over.

## Published event

Sent to SNS topic `order-file-reconciliation-batch`:

```json
{
  "batchId": "b9d4e9c7-3e8e-4cb7-9a35-7d5cf9c1bb2c",
  "records": 500,
  "bucket": "order-file-upload-s3",
  "key": "provider-a/file-2026-05-18.csv",
  "provider": "provider-a",
  "receivedAt": "2026-05-19T12:34:56Z"
}
```

Message attributes: `provider`, `eventType = ReconciliationBatchReadyEvent`,
so downstream SQS subscriptions can apply SNS filter policies.

## Environment variables

| Variable                   | Description                                        |
|----------------------------|----------------------------------------------------|
| `RECONCILIATION_TOPIC_ARN` | SNS topic ARN to publish batch events             |
| `ALLOWED_PROVIDERS`        | Comma-separated provider allow-list               |
| `MAX_FILE_SIZE_BYTES`      | Hard upper bound for accepted CSV files           |
| `ENVIRONMENT`              | Logical environment tag (dev/stg/prd)             |

## Build

```bash
dotnet tool install -g Amazon.Lambda.Tools   # one-time
cd services/OrderFileIngestion
./build.sh
# produces ./artifacts/order-file-ingestion-lambda.zip (path consumed by Terraform)
```

## Test

```bash
dotnet test services/OrderFileIngestion/OrderFileIngestion.sln
```
