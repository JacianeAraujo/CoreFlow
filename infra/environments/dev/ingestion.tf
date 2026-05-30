############################################
# Reconciliation file ingestion stack
#
# Event flow:
#   CSV uploaded -> S3 (order-file-upload-s3)
#                -> S3 event notification
#                -> Lambda (order-file-ingestion-lambda)
#                -> SNS topic (order-file-reconciliation-batch)
#                -> [future] SQS queues consumed by ECS workers
############################################

data "aws_caller_identity" "current" {}
data "aws_region" "current" {}

locals {
  ingestion_tags = merge(var.common_tags, {
    Component = "order-file-ingestion"
    Env       = var.environment
  })

  # Computed from inputs to avoid a cycle between the queue (whose policy
  # references the topic ARN) and the topic (whose subscription references
  # the queue ARN).
  reconciliation_topic_arn = "arn:aws:sns:${data.aws_region.current.name}:${data.aws_caller_identity.current.account_id}:${var.ingestion_sns_topic_name}"
}

module "reconciliation_worker_queue" {
  source = "../../modules/sqs"

  queue_name              = var.reconciliation_worker_queue_name
  subscription_topic_arns = [local.reconciliation_topic_arn]
  tags                    = local.ingestion_tags
}

module "reconciliation_topic" {
  source = "../../modules/sns"

  topic_name = var.ingestion_sns_topic_name
  tags       = local.ingestion_tags

  sqs_subscriptions = [
    {
      name      = "reconciliation-worker"
      queue_arn = module.reconciliation_worker_queue.queue_arn
    }
  ]
}

module "ingestion_lambda" {
  source = "../../modules/lambda"

  function_name = var.ingestion_lambda_name
  handler       = "OrderFileIngestion::CoreFlow.OrderFileIngestion.Function::HandleAsync"
  runtime       = "dotnet8"
  memory_size   = 512
  timeout       = 60
  package_path  = var.ingestion_lambda_package_path

  s3_read_arns     = [module.order_file_bucket.bucket_arn]
  sns_publish_arns = [module.reconciliation_topic.topic_arn]

  environment_variables = {
    RECONCILIATION_TOPIC_ARN = module.reconciliation_topic.topic_arn
    ALLOWED_PROVIDERS        = var.allowed_providers
    MAX_FILE_SIZE_BYTES      = tostring(var.max_file_size_bytes)
    ENVIRONMENT              = var.environment
  }

  tags = local.ingestion_tags
}

module "order_file_bucket" {
  source = "../../modules/s3-bucket"

  bucket_name        = var.order_file_bucket_name
  versioning_enabled = true
  tags               = local.ingestion_tags

  lambda_notifications = [
    {
      lambda_arn           = module.ingestion_lambda.function_arn
      lambda_function_name = module.ingestion_lambda.function_name
      events               = ["s3:ObjectCreated:*"]
      filter_suffix        = ".csv"
    }
  ]
}
