variable "function_name" {
  description = "Name of the Lambda function."
  type        = string
}

variable "handler" {
  description = "Lambda handler entry point. For .NET: \"<Assembly>::<Namespace.Type>::FunctionHandler\"."
  type        = string
}

variable "runtime" {
  description = "Lambda runtime identifier (e.g. dotnet8)."
  type        = string
  default     = "dotnet8"
}

variable "architectures" {
  description = "Instruction set architecture."
  type        = list(string)
  default     = ["x86_64"]
}

variable "memory_size" {
  description = "Memory allocated to the function (MB)."
  type        = number
  default     = 512
}

variable "timeout" {
  description = "Function timeout in seconds."
  type        = number
  default     = 30
}

variable "package_path" {
  description = "Path to the deployment .zip artifact."
  type        = string
}

variable "environment_variables" {
  description = "Environment variables exposed to the function."
  type        = map(string)
  default     = {}
}

variable "log_retention_days" {
  description = "CloudWatch Logs retention in days."
  type        = number
  default     = 14
}

variable "s3_read_arns" {
  description = "Bucket ARNs the function may read from."
  type        = list(string)
  default     = []
}

variable "sns_publish_arns" {
  description = "SNS topic ARNs the function may publish to."
  type        = list(string)
  default     = []
}

variable "tags" {
  description = "Tags applied to all resources."
  type        = map(string)
  default     = {}
}
