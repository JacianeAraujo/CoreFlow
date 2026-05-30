output "order_file_bucket_name" {
  description = "Bucket receiving reconciliation CSV uploads."
  value       = module.order_file_bucket.bucket_id
}

output "order_file_bucket_arn" {
  description = "ARN of the upload bucket."
  value       = module.order_file_bucket.bucket_arn
}

output "ingestion_lambda_name" {
  description = "Name of the ingestion Lambda."
  value       = module.ingestion_lambda.function_name
}

output "ingestion_lambda_arn" {
  description = "ARN of the ingestion Lambda."
  value       = module.ingestion_lambda.function_arn
}

output "reconciliation_topic_arn" {
  description = "SNS topic ARN for reconciliation batch events."
  value       = module.reconciliation_topic.topic_arn
}

output "reconciliation_worker_queue_url" {
  description = "URL of the SQS queue consumed by the reconciliation worker."
  value       = module.reconciliation_worker_queue.queue_url
}

output "reconciliation_worker_queue_arn" {
  description = "ARN of the SQS queue consumed by the reconciliation worker."
  value       = module.reconciliation_worker_queue.queue_arn
}
