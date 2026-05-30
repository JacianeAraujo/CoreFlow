module "orders_queue" {
  source = "../../modules/sqs"

  queue_name = "orders-dev"
  dlq_name   = "orders-dlq"
}
