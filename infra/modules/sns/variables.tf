variable "topic_name" {
  description = "Name of the SNS topic."
  type        = string
}

variable "tags" {
  description = "Tags applied to the topic."
  type        = map(string)
  default     = {}
}

variable "policy_json" {
  description = "Optional JSON policy attached to the topic."
  type        = string
  default     = null
}

variable "sqs_subscriptions" {
  description = <<EOT
List of SQS subscriptions. Each item supports:
  - name (string)                       : Logical name (used as map key)
  - queue_arn (string)                  : Target SQS queue ARN
  - raw_message_delivery (bool, optional): Defaults to true
  - filter_policy (string, optional)    : JSON-encoded SNS filter policy
EOT
  type        = any
  default     = []
}
