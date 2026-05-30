data "aws_caller_identity" "current" {}
data "aws_region" "current" {}

############################################
# IAM role for Lambda
############################################

data "aws_iam_policy_document" "assume" {
  statement {
    actions = ["sts:AssumeRole"]
    principals {
      type        = "Service"
      identifiers = ["lambda.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "this" {
  name               = "${var.function_name}-role"
  assume_role_policy = data.aws_iam_policy_document.assume.json
  tags               = var.tags
}

############################################
# CloudWatch Logs - least privilege scoped to this function's log group
############################################

resource "aws_cloudwatch_log_group" "this" {
  name              = "/aws/lambda/${var.function_name}"
  retention_in_days = var.log_retention_days
  tags              = var.tags
}

data "aws_iam_policy_document" "logs" {
  statement {
    actions = [
      "logs:CreateLogStream",
      "logs:PutLogEvents",
    ]
    resources = ["${aws_cloudwatch_log_group.this.arn}:*"]
  }
}

resource "aws_iam_role_policy" "logs" {
  name   = "${var.function_name}-logs"
  role   = aws_iam_role.this.id
  policy = data.aws_iam_policy_document.logs.json
}

############################################
# S3 read access (least privilege - only specified buckets/prefixes)
############################################

data "aws_iam_policy_document" "s3_read" {
  count = length(var.s3_read_arns) > 0 ? 1 : 0

  statement {
    sid       = "ListBuckets"
    actions   = ["s3:ListBucket", "s3:GetBucketLocation"]
    resources = var.s3_read_arns
  }

  statement {
    sid     = "ReadObjects"
    actions = ["s3:GetObject", "s3:GetObjectVersion"]
    resources = [
      for arn in var.s3_read_arns : "${arn}/*"
    ]
  }
}

resource "aws_iam_role_policy" "s3_read" {
  count = length(var.s3_read_arns) > 0 ? 1 : 0

  name   = "${var.function_name}-s3-read"
  role   = aws_iam_role.this.id
  policy = data.aws_iam_policy_document.s3_read[0].json
}

############################################
# SNS publish access (least privilege - only specified topics)
############################################

data "aws_iam_policy_document" "sns_publish" {
  count = length(var.sns_publish_arns) > 0 ? 1 : 0

  statement {
    actions   = ["sns:Publish"]
    resources = var.sns_publish_arns
  }
}

resource "aws_iam_role_policy" "sns_publish" {
  count = length(var.sns_publish_arns) > 0 ? 1 : 0

  name   = "${var.function_name}-sns-publish"
  role   = aws_iam_role.this.id
  policy = data.aws_iam_policy_document.sns_publish[0].json
}

############################################
# Lambda function
############################################

resource "aws_lambda_function" "this" {
  function_name = var.function_name
  role          = aws_iam_role.this.arn
  handler       = var.handler
  runtime       = var.runtime
  architectures = var.architectures
  memory_size   = var.memory_size
  timeout       = var.timeout

  filename         = var.package_path
  source_code_hash = filebase64sha256(var.package_path)

  environment {
    variables = var.environment_variables
  }

  tags = var.tags

  depends_on = [aws_cloudwatch_log_group.this]
}
