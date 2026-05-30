locals {
  dlq_name = coalesce(var.dlq_name, "${var.queue_name}-dlq")
}

resource "aws_sqs_queue" "dlq" {
  name = local.dlq_name
  tags = var.tags
}

resource "aws_sqs_queue" "this" {
  name = var.queue_name
  tags = var.tags

  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.dlq.arn
    maxReceiveCount     = var.max_receive_count
  })
}

data "aws_iam_policy_document" "from_sns" {
  count = length(var.subscription_topic_arns) > 0 ? 1 : 0

  statement {
    sid       = "AllowSNSPublish"
    actions   = ["sqs:SendMessage"]
    resources = [aws_sqs_queue.this.arn]

    principals {
      type        = "Service"
      identifiers = ["sns.amazonaws.com"]
    }

    condition {
      test     = "ArnEquals"
      variable = "aws:SourceArn"
      values   = var.subscription_topic_arns
    }
  }
}

resource "aws_sqs_queue_policy" "from_sns" {
  count     = length(var.subscription_topic_arns) > 0 ? 1 : 0
  queue_url = aws_sqs_queue.this.id
  policy    = data.aws_iam_policy_document.from_sns[0].json
}
