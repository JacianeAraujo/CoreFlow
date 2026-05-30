variable "queue_name" {
  type = string
}

variable "dlq_name" {
  description = "Optional DLQ name. Defaults to \"<queue_name>-dlq\"."
  type        = string
  default     = null
}

variable "max_receive_count" {
  description = "Number of receives before a message moves to the DLQ."
  type        = number
  default     = 3
}

variable "tags" {
  description = "Tags applied to the queue and its DLQ."
  type        = map(string)
  default     = {}
}

variable "subscription_topic_arns" {
  description = "SNS topic ARNs allowed to send messages to this queue (used to author the queue's resource policy)."
  type        = list(string)
  default     = []
}
