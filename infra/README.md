# Infrastructure (Terraform)

Terraform definitions for CoreFlow's AWS footprint.

## Layout

```
infra/
├── modules/
│   ├── s3-bucket/     # private, encrypted bucket + optional Lambda notifications
│   ├── sns/           # topic + optional SQS subscriptions
│   ├── sqs/           # queue + DLQ
│   └── lambda/        # function + IAM role + scoped S3/SNS policies + log group
└── environments/
    └── dev/
        ├── providers.tf
        ├── variables.tf
        ├── main.tf            # orders queue
        ├── ingestion.tf       # reconciliation ingestion stack
        └── outputs.tf
```

Each module exposes only what its callers need (`outputs.tf`) and stays
free of environment-specific values (`variables.tf`).

## Reconciliation ingestion stack

`environments/dev/ingestion.tf` wires three modules:

1. **`sns`** — creates topic `order-file-reconciliation-batch`. SQS
   subscriptions are scaffolded (commented) so ECS worker queues can be
   plugged in later without re-architecting.
2. **`lambda`** — deploys `order-file-ingestion-lambda` (dotnet8) from the
   pre-built artifact at
   `services/OrderFileIngestion/artifacts/order-file-ingestion-lambda.zip`.
   The IAM role is created in-module with least-privilege policies:
   - `logs:CreateLogStream` / `logs:PutLogEvents` scoped to its own log group
   - `s3:GetObject` scoped to the upload bucket only
   - `sns:Publish` scoped to the reconciliation topic only
3. **`s3-bucket`** — creates `order-file-upload-s3` (private, AES256,
   versioned) plus an `s3:ObjectCreated:*` notification filtered to `.csv`
   suffix that invokes the Lambda. A matching
   `aws_lambda_permission` is created so S3 is allowed to invoke it.

### Provisioning order

Terraform resolves the dependency graph automatically:

```
sns topic ─┐
            ├─▶ lambda (needs topic ARN + bucket ARN for IAM)
s3 bucket ─┤        │
            └────────┴─▶ s3 bucket notification + lambda permission
```

## Deploying

```bash
# 1. Build the Lambda artifact first — Terraform reads the .zip
cd services/OrderFileIngestion && ./build.sh && cd -

# 2. Apply
cd infra/environments/dev
terraform init
terraform plan
terraform apply
```

## Inputs (dev environment)

| Variable                       | Default                                                                   |
|--------------------------------|---------------------------------------------------------------------------|
| `aws_region`                   | `us-east-1`                                                               |
| `environment`                  | `dev`                                                                     |
| `order_file_bucket_name`       | `order-file-upload-s3`                                                    |
| `ingestion_lambda_name`        | `order-file-ingestion-lambda`                                             |
| `ingestion_sns_topic_name`     | `order-file-reconciliation-batch`                                         |
| `ingestion_lambda_package_path`| `../../../services/OrderFileIngestion/artifacts/order-file-ingestion-lambda.zip` |
| `allowed_providers`            | `provider-a,provider-b,provider-c`                                        |
| `max_file_size_bytes`          | `52428800` (50 MiB)                                                       |

## Outputs

| Output                     | Description                                  |
|----------------------------|----------------------------------------------|
| `order_file_bucket_name`   | Upload bucket name                           |
| `order_file_bucket_arn`    | Upload bucket ARN                            |
| `ingestion_lambda_name`    | Lambda function name                         |
| `ingestion_lambda_arn`     | Lambda function ARN                          |
| `reconciliation_topic_arn` | SNS topic ARN for downstream consumers       |

## Extending later: ECS workers via SQS

The Lambda's responsibility ends at publishing the
`ReconciliationBatchReadyEvent` to SNS. Heavy work — fetching the file
from S3, parsing every row, calling pricing/reconciliation services,
persisting results — belongs in long-running ECS workers, not in Lambda.

The standard fan-out pattern is:

```
SNS topic: order-file-reconciliation-batch
    │
    ├──▶ SQS: reconciliation-worker  (+ DLQ)  ─▶ ECS Fargate task (worker)
    └──▶ SQS: audit-trail            (+ DLQ)  ─▶ ECS Fargate task (audit)
```

Why route SNS → SQS → ECS instead of SNS → ECS directly:

- **Buffering**: a burst of uploads spikes SNS but SQS absorbs it; ECS
  workers pull at their own pace.
- **At-least-once delivery + DLQ**: failed messages return to the queue
  (visibility timeout) and end up in the DLQ after `maxReceiveCount`
  retries — already wired in the `sqs` module.
- **Independent scaling per consumer**: each SQS-bound service can scale
  on `ApproximateNumberOfMessagesVisible` independently.
- **Filter policies on SNS subscriptions**: a worker can subscribe only
  to messages where `provider = provider-a`, for example, without the
  publisher knowing.

To wire a consumer in Terraform:

```hcl
module "reconciliation_worker_queue" {
  source     = "../../modules/sqs"
  queue_name = "reconciliation-worker"
}

module "reconciliation_topic" {
  source     = "../../modules/sns"
  topic_name = var.ingestion_sns_topic_name

  sqs_subscriptions = [
    {
      name      = "reconciliation-worker"
      queue_arn = module.reconciliation_worker_queue.queue_arn
      # Optional: only deliver messages from provider-a
      # filter_policy = jsonencode({ provider = ["provider-a"] })
    }
  ]
}
```

Two extra pieces are needed for an end-to-end ECS consumer (not in this
ingestion stack so we don't bloat the Lambda's scope):

1. **SQS queue policy** allowing the SNS topic to `sqs:SendMessage`.
2. **ECS task** running a .NET `BackgroundService` (matching CoreFlow's
   convention) that long-polls the queue and processes each message
   inside a `try/catch` so failures are visible and DLQ-bound, not
   silently lost.

This keeps the ingestion Lambda fast and cheap, and shifts the expensive
work to a horizontally scalable compute tier.
