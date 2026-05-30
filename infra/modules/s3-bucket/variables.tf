variable "bucket_name" {
  description = "Name of the S3 bucket."
  type        = string
}

variable "versioning_enabled" {
  description = "Enable S3 object versioning."
  type        = bool
  default     = true
}

variable "tags" {
  description = "Tags to apply to the bucket."
  type        = map(string)
  default     = {}
}

variable "lambda_notifications" {
  description = <<EOT
List of Lambda notification configurations. Each item supports:
  - lambda_arn (string)             : Target Lambda ARN
  - lambda_function_name (string)   : Target Lambda function name (for permissions)
  - events (list(string))           : S3 event types (e.g. ["s3:ObjectCreated:*"])
  - filter_prefix (string, optional): Key prefix filter
  - filter_suffix (string, optional): Key suffix filter
EOT
  type        = any
  default     = []
}
