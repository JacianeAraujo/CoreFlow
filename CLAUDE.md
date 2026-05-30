# CoreFlow

CoreFlow is a distributed microservices-based application built with .NET 9 and AWS to practice modern backend and cloud architecture.

# Main Goal

Practice and demonstrate knowledge of:

- Microservices architecture
- Event-driven architecture
- CQRS
- Asynchronous communication
- AWS SNS
- AWS SQS
- DynamoDB
- RDS PostgreSQL
- Docker
- EC2

# Services

The system contains the following microservices:

- Order Service
- Notification Service
- Inventory Service

# System Flow

1. Order Service receives orders
2. Orders are stored in PostgreSQL
3. OrderCreatedEvent is published to SNS
4. SNS distributes messages to SQS queues
5. Notification Service processes notifications
6. Inventory Service updates stock

# Technology Stack

- .NET 9
- ASP.NET Core Minimal APIs
- Entity Framework Core
- PostgreSQL
- DynamoDB
- AWS SDK for .NET
- Docker Compose
- CQRS
- MediatR

# Architecture

Each microservice must contain:

- its own solution
- its own tests
- its own Dockerfile
- independent deployment capability

Internal architecture:

- Api
- Application
- Domain
- Infrastructure

# Domain Rules

- Domain entities must encapsulate business rules
- Avoid anemic domain models
- Domain layer must not depend on Infrastructure

# CQRS Rules

- Commands for write operations
- Queries for read operations
- Handlers must be separated by feature
- Keep CQRS implementation lightweight

# Naming Conventions

- Events must end with "Event"
- Consumers must end with "Consumer"
- DTOs must end with "Dto"
- Commands must end with "Command"
- Queries must end with "Query"

# Infrastructure

- PostgreSQL running with Docker
- SNS/SQS/DynamoDB hosted on AWS
- Deployment using EC2 + Docker Compose

# Development Rules

- Use async/await
- Use CancellationToken
- Use BackgroundService for message consumers
- All code, comments, documentation and identifiers must be written in English
- Avoid unnecessary abstractions
- Avoid Generic Repository pattern
- Prioritize simplicity and readability
- Prioritize interview-explainable code