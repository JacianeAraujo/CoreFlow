module "orders_queue" {
  source = "../../modules/sqs"

  queue_name = "orders-dev"
}
