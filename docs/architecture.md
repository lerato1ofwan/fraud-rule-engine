# Fraud Rule Engine - Architecture Documentation

## Overview

The Fraud Rule Engine is a demo system built with .NET 8, following Domain-Driven Design (DDD), CQRS, and event-driven architecture patterns. The system ingests transactions, evaluates them against fraud rules, and provides reporting capabilities.

<!-- Diagrams to add -->
<!-- Will add diagram here if there's time for the system and how the differenc compoennts communicate  -->
<!-- Will add diagram here if there's time for production setup, using managed services like RDS,MSK,CloudwatchLogs etc - should simplify the number of containers currently there  -->

## System Architecture

### Microservices

#### 1. FraudRuleEngine.Transactions.Api
**Purpose**: Ingress service for receiving and storing transactions.

**Responsibilities**:
- Receives transaction requests via REST API
- Validates and stores transactions
- Publishes `transaction.received` events to Kafka
- Implements idempotency using external transaction IDs
- Wraps persistence and outbox writes in a unit-of-work for atomic commits
- Uses Outbox pattern for reliable event publishing

**Key Components**:
- **Domain Layer**: Transaction entity, TransactionMetadata value object, domain events
- **Application Layer**: Commands (CreateTransaction), Queries (GetTransaction), handlers with MediatR
- **Infrastructure Layer**: EF Core DbContext, Repository pattern, Kafka producer, Outbox service
- **API Layer**: Controller API endpoints with Problem Details error handling
- **Hosted Services**: OutboxPublisher (polls and publishes outbox messages)
- **Resilience**: Polly retry policies, DLQ fallback, exponential backoff

**Database**: PostgreSQL (`transactions_db`)
- Tables: `transactions`, `transaction_ingest_audit`, `outbox_messages`

#### 2. FraudRuleEngine.Evaluations.Worker
**Purpose**: Background worker that consumes transaction events and evaluates fraud rules.

**Responsibilities**:
- Consumes `transaction.received` events from Kafka
- Executes fraud rule engine with multiple rule strategies
- Stores fraud check results
- Publishes `fraud.assessed` events to Kafka

**Key Components**:
- **Domain Layer**: FraudCheck entity, FraudRuleResult value object
- **Rules Engine**: Strategy pattern for individual rules, Composite pattern for rule pipeline, Specification pattern for conditional logic, Mediator patterns for rule data pre-fetching
- **Rules Implemented**:
  - `HighAmountRule`: Flags transactions exceeding a threshold
  - `VelocityRule`: Detects high-frequency transactions
  - `ForeignCountryRule`: Identifies transactions from foreign countries
- **Infrastructure Layer**: EF Core DbContext, Kafka consumer/producer

**Database**: PostgreSQL (`fraud_rule_engine_db`)
- Tables: `fraud_checks`, `fraud_rule_results`, `fraud_rules_metadata`

#### 3. FraudRuleEngine.Reporting.Api
**Purpose**: Reporting and analytics service with read-optimized data model.

**Responsibilities**:
- Consumes `fraud.assessed` events from Kafka
- Builds materialized read models for reporting
- Exposes query endpoints for fraud statistics
<!-- - Supports event replay for rebuilding projections -->

**Key Components**:
- **Read Models**: FraudSummary, FraudRuleHeatmap
- **Projections**: FraudAssessedProjection for event-to-read-model transformation
- **Query Layer**: CQRS read side with MediatR queries
- **Endpoints**:
  - `GET /fraud/summary/{transactionId}`: Get fraud summary for a transaction
  - `GET /fraud/stats/daily`: Get daily fraud statistics
  - `GET /fraud/rules/top`: Get top triggered rules

**Database**: PostgreSQL (`reporting_db`)
- Tables: `fraud_summary`, `fraud_rule_heatmap`

### Shared Library

**FraudRuleEngine.Shared**: Contains contracts, events, and messaging infrastructure shared across services.

**Components**:
- **Contracts**: TransactionReceived, FraudAssessed
- **Events**: IDomainEvent interface, EventTypes constants
- **Messaging**: IEventProducer, IEventConsumer, Kafka implementations, KafkaTopics constants
- **Common**: Result pattern for error handling
- **Metrics**: Shared Prometheus metrics (FraudMetrics)

## Design Patterns

### Domain-Driven Design (DDD)
- **Entities**: Transaction, FraudCheck
- **Value Objects**: TransactionMetadata, FraudRuleResult
- **Domain Events**: TransactionReceivedEvent
- **Repositories**: Abstraction for data access

### CQRS (Command Query Responsibility Segregation)
- **Commands**: Write operations (CreateTransaction)
- **Queries**: Read operations (GetTransaction, GetFraudSummary)
- **Separate Models**: Write model in Transactions API, read model in Reporting API

### Event-Driven Architecture
- **Kafka Topics** (centralized in `KafkaTopics` class):
  - `transaction.received`: Published by Transactions API
  - `fraud.assessed`: Published by Evaluations Worker
  - `dlq` (Dead Letter Queue): Failed messages after all retries
- **Event Types** (centralized in `EventTypes` class):
  - `TransactionReceivedEvent`: Domain event for transaction creation
  - `FraudAssessedEvent`: Domain event for fraud assessment completion
- **Event Sourcing**: Events stored in outbox for reliable delivery
- **Dead Letter Queue (DLQ)**: Failed messages published to DLQ with metadata (original topic, failure reason, timestamp)

### Strategy Pattern
- Individual fraud rules implement `IFraudRule` interface
- Rules can be added/removed without modifying the pipeline

### Composite Pattern
- `CompositeRulePipeline` aggregates multiple rules
- Evaluates all rules and combines results

### Specification Pattern
- `ISpecification<T>` for conditional rule evaluation
- Example: `HighRiskSpecification` filters high-risk results

### Repository Pattern
- Abstraction over data access
- Enables testability and flexibility

### Outbox Pattern
- Events stored in database within same transaction
- `OutboxPublisher` (IHostedService) polls outbox table every 15 seconds
- Processes messages in batches of 100
- Marks messages as processed only after successful Kafka publish
- Ensures at-least-once delivery
- Atomicity: Transaction and outbox message committed together or rolled back together

### Result Pattern
- Functional error handling without exceptions
- `Result<T>` and `Result` classes for success/failure scenarios
- Prevents exception-based control flow
- Type-safe error handling

## Infrastructure

### Databases
- **PostgreSQL 16**: Three separate databases for each service
- **EF Core Migrations**: Automatic schema management

### Message Broker
- **Apache Kafka**: Event streaming platform
- **Kafka UI**: Web interface for topic management (port 8080)

### Monitoring & Observability
- **Prometheus**: Metrics collection (port 9090)
- **Grafana**: Visualization and dashboards (port 3000)
- **OpenTelemetry**: Distributed tracing and metrics
  - Automatic instrumentation for HTTP, EF Core, Kafka
  - Custom activity sources for Kafka producer/consumer
  - Trace context propagation through Kafka headers
- **Jaeger**: Distributed tracing visualization (port 16686)
- **Loki**: Log aggregation (with Promtail)
- **Promtail**: Log shipping to Loki

### Resilience
- **Polly Retry Policies**: 
  - Exponential backoff (3 retries: 2s, 4s, 8s delays)
  - Applied to Kafka producer operations
  - Applied to database operations in OutboxPublisher
- **Dead Letter Queue (DLQ)**: 
  - Failed messages after all retries published to DLQ
  - DLQ messages include metadata: original topic, payload, failure reason, timestamp, exception type
  - Prevents message loss
- **Health Checks**: 
  - Database health checks (PostgreSQL)
  - Kafka health checks (using AspNetCore.HealthChecks.Kafka)
  - `/health` endpoint on all APIs
- **Graceful Shutdown**: Proper cancellation token handling in hosted services

## Technology Stack

- **.NET 8**: Runtime and SDK
- **ASP.NET Core**: Web API framework
- **EF Core**: ORM for database access
- **MediatR**: Mediator pattern for CQRS
- **FluentValidation**: Input validation
- **Confluent.Kafka**: Kafka client library
- **Polly**: Resilience and fault tolerance (retry policies, circuit breakers)
- **OpenTelemetry**: Distributed tracing and metrics
- **Swagger/OpenAPI**: API documentation
- **Docker & Docker Compose**: Containerization and orchestration
- **PostgreSQL 16**: Database
- **Apache Kafka**: Event streaming platform

## Running

### Docker Compose
Two deployment options available:

**Option 1: Simplified Setup (7 containers)**
```bash
docker-compose -f docker-compose.development.yml up -d
```
Includes: PostgreSQL, Kafka, 3 application services, Prometheus, Grafana

**Option 2: Full Observability Stack (13 containers)**
```bash
docker-compose up -d
```
Includes everything above plus: Loki, Promtail, Jaeger, Kafka UI

### Service Ports
- Transactions API: `5000`
- Reporting API: `5001`
- Kafka UI: `8080`
- Prometheus: `9090`
- Grafana: `3000`
- Jaeger: `16686`
- PostgreSQL: `5432` (default, configurable via `.env`)
- Kafka: `9092` (internal), `29092` (external)

## Data Flow

1. **Transaction Ingestion**:
   - Client → Transactions API → PostgreSQL → Outbox → Kafka (`transaction.received`)

2. **Fraud Evaluation**:
   - Kafka (`transaction.received`) → Evaluations Worker → Rule Engine → PostgreSQL → Kafka (`fraud.assessed`)
   - On failure: Message published to DLQ with retry metadata

3. **Reporting**:
   - Kafka (`fraud.assessed`) → Reporting Worker → Projection → PostgreSQL (Read Model) → Reporting API
   - On failure: Exception logged, worker continues processing

### Transaction Ingestion Flow Details

1. `TransactionsController` receives the HTTP POST and sends a `CreateTransactionCommand` via MediatR.
2. `CreateTransactionCommandHandler` runs inside `TransactionUnitOfWork`, which starts an EF Core transaction and defers `SaveChangesAsync`/`Commit` until the entire pipeline succeeds.
3. The handler performs an idempotency check on `ExternalId`, creates the `Transaction` aggregate, and stages it in the context through `ITransactionRepository`.
4. Domain events raised by the aggregate are immediately forwarded to `IOutboxService.AddToOutboxAsync`, which stages a serialized message in the `outbox_messages` table using the same DbContext instance.
5. `TransactionUnitOfWork` commits once—persisting both the transaction row and the outbox record or rolling back both in case of a failure—logging and returning a `Result<Guid>` to the API.
6. `OutboxPublisher` (IHostedService) polls `outbox_messages` every 15 seconds and publishes the payload to the `transaction.received` Kafka topic using `KafkaEventProducer`.
7. `KafkaEventProducer` implements resilience:
   - Retries with exponential backoff (3 retries: 2s, 4s, 8s delays)
   - On all retry failures, publishes message to DLQ (`dlq` topic) with metadata
   - DLQ message includes: original topic, payload, failure reason, timestamp, exception type
8. The Evaluations Worker consumes `transaction.received` events and starts rule evaluation.

This sequence guarantees that:
- Rule evaluation always trails a durable transaction write, even if Kafka or the worker is temporarily unavailable
- No messages are lost (DLQ fallback ensures message preservation)
- Actionable errors are surfaced to clients through the `Result` abstraction
- Atomicity is maintained: transaction and outbox message are committed or rolled back together

## Scalability Considerations

- **Horizontal Scaling**: Each service can be scaled independently
- **Kafka Consumer Groups**: Multiple worker instances can process events in parallel
- **Database Sharding**: Each service has its own database
- **Read Replicas**: Reporting database can have read replicas for query performance

## Security Considerations

- **Input Validation**: All API inputs validated
- **Idempotency**: Duplicate transactions handled gracefully
- **Error Handling**: Problem Details standard for API errors
- **Health Checks**: Monitoring service availability

## Future Enhancements

- Event replay capability for rebuilding projections
- More sophisticated fraud rules
- Machine learning integration
- Real-time alerting
- Audit logging
- API authentication/authorization
- More test coverage (Reporting API and Evaluations Worker integration tests)
- Schema registry for Kafka events
- Materialized views for reporting performance

## Production Deployment

For production deployment architecture, infrastructure as code (Terraform), CI/CD pipelines, and AWS services (EKS, RDS, MSK), this is all part of future work.
