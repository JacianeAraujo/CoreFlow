resource "aws_sns_topic" "this" {
  name = var.topic_name
  tags = var.tags
}

resource "aws_sns_topic_policy" "this" {
  count = var.policy_json == null ? 0 : 1

  arn    = aws_sns_topic.this.arn
  policy = var.policy_json
}

resource "aws_sns_topic_subscription" "sqs" {
  for_each = {
    for s in var.sqs_subscriptions : s.name => s
  }

  topic_arn            = aws_sns_topic.this.arn
  protocol             = "sqs"
  endpoint             = each.value.queue_arn
  raw_message_delivery = lookup(each.value, "raw_message_delivery", true)

  filter_policy = lookup(each.value, "filter_policy", null)
}
