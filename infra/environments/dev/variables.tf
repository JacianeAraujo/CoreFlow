variable "aws_region" {
  description = "AWS region."
  type        = string
  default     = "us-east-1"
}

variable "environment" {
  description = "Environment name."
  type        = string
  default     = "dev"
}

variable "order_file_bucket_name" {
  description = "S3 bucket where reconciliation CSV files land."
  type        = string
  default     = "order-file-upload-s3"
}

variable "ingestion_lambda_name" {
  description = "Name of the ingestion Lambda function."
  type        = string
  default     = "order-file-ingestion-lambda"
}

variable "ingestion_sns_topic_name" {
  description = "SNS topic name for reconciliation batch events."
  type        = string
  default     = "order-file-reconciliation-batch"
}

variable "reconciliation_worker_queue_name" {
  description = "SQS queue consumed by the reconciliation worker."
  type        = string
  default     = "reconciliation-worker-dev"
}

variable "ingestion_lambda_package_path" {
  description = "Path to the pre-built Lambda deployment .zip."
  type        = string
  default     = "../../../services/OrderFileIngestion/artifacts/order-file-ingestion-lambda.zip"
}

variable "allowed_providers" {
  description = "Comma-separated list of accepted provider codes for filename validation."
  type        = string
  default     = "provider-a,provider-b,provider-c"
}

variable "max_file_size_bytes" {
  description = "Maximum accepted file size (bytes) before the Lambda rejects the upload."
  type        = number
  default     = 52428800
}

variable "common_tags" {
  description = "Tags applied to all resources."
  type        = map(string)
  default = {
    Project = "CoreFlow"
    Owner   = "platform"
  }
}
