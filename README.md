# Fraud Rule Engine

A demo system for fraud detection using a custom rule engine implementation, built with .NET 8, following Domain-Driven Design (DDD), CQRS, and microservices with event-driven architecture patterns.

## 🏗️ Architecture

The system consists of three microservices:

1. **Transactions API**: Ingress service for receiving and storing transactions
2. **Evaluations Worker**: Background worker that evaluates transactions against fraud rules
3. **Reporting API**: Analytics service with read-optimized data models and queries

## 🚀 Quick Start

### Prerequisites

- .NET 8 SDK
- Docker and Docker Compose
- Git

### Initial Setup

**⚠️ Important**: All credentials and configuration are loaded from a `.env` file. This ensures no sensitive data is committed to version control.

1. **Clone the repository**:
   ```bash
   git clone https://github.com/your-org/fraud-rule-engine.git
   cd fraud-rule-engine
   ```

2. **Create your environment file**:
   ```bash
   cp .env.example .env
   ```

3. **Configure your environment variables**:
   Open `.env` in your editor and update the following **mandatory** values:
   - `POSTGRES_PASSWORD`: Set a secure password for PostgreSQL (required)
   - `GRAFANA_ADMIN_PASSWORD`: Set a password for Grafana admin user (required)
   
   **Optional**: For development, you can use the default values provided in `.env.example`. For production, use strong, unique passwords.

   > **Note**: The `.env` file is already in `.gitignore` and will not be committed to version control.

4. **Start the application stack**:
   ```bash
   docker compose up --build -d
   ```

5. **Verify services are running**:
   ```bash
   docker compose ps
   ```

6. **View logs** (optional):
   ```bash
   docker compose logs -f
   ```

### Service Endpoints

Once all services are running, you can access:

- **Transactions API**: http://localhost:5000/swagger
- **Reporting API**: http://localhost:5001/swagger
- **Kafka UI**: http://localhost:8080
- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3000 (credentials from `.env`)

### Stopping Services

```bash
# Stop all services
docker compose down

# Stop and remove volumes
docker compose down -v
```

### API Documentation

- **Transactions API Swagger**: http://localhost:5000/swagger
- **Reporting API Swagger**: http://localhost:5001/swagger

## 📋 Features

### Transactions API
- REST API for transaction ingestion
- Idempotency using external transaction IDs
- Outbox pattern for reliable transactions saving and event publishing
- Domain-Driven Design with entities and value objects
- CQRS with MediatR
- Problem Details for error handling
- Health checks

### Evaluations Worker
- Kafka consumer for transaction events
- Fraud rule engine with Strategy pattern
- Composite rule pipeline
- Specification pattern for conditional logic
- Request/RequestHandler Mediator pattern for data loading required by rules
- Multiple fraud rules to flag transactions
- Kafka producer for fraud assessment events

### Reporting API
- Kafka consumer for fraud assessment events
- Event projections for read models
- CQRS read side with optimized queries
- Analytics endpoints (summary, daily stats, top rules)
<!-- - Materialized views for performance @Todo: will see if there'll be enough time to implement  -->

### Infrastructure
- **Single PostgreSQL instance** shared by all services, with separate databases per application:
  - `transactions_db` - Transactions API database
  - `fraud_rule_engine_db` - Evaluations Worker database
  - `reporting_db` - Reporting API database
- Apache Kafka with Zookeeper
- Kafka UI for topic management
- Prometheus for metrics
- Grafana for visualization
- Docker Compose orchestration

## 🧪 Testing

### Create a Transaction

```bash
curl -X POST http://localhost:5000/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "accountId": "123e4567-e89b-12d3-a456-426614174000",
    "amount": 15000,
    "merchantId": "123e4567-e89b-12d3-a456-426614174001",
    "currency": "ZAR",
    "timestamp": "2024-01-01T12:00:00Z",
    "externalId": "txn-12345",
    "metadata": {
      "Country": "RSA",
      "IPAddress": "192.168.1.1"
    }
  }'
```

### Get Fraud Summary

```bash
curl http://localhost:5001/fraud/summary/{transactionId}
```

### Get Daily Stats

```bash
curl http://localhost:5001/fraud/stats/daily?date=2024-01-01
```

### Get Top Rules

```bash
curl http://localhost:5001/fraud/rules/top?top=10
```

## 📚 Documentation

- [Architecture Documentation](docs/architecture.md)
- [Event Documentation](docs/events.md)
- [Rules Documentation](docs/rules.md)

## 🛠️ Tech Stack

- **.NET 8**: Runtime and SDK
- **EF Core**: Object-relational mapping
- **MediatR**: CQRS implementation
- **Confluent.Kafka**: Kafka client
- **Polly**: Resilience and fault tolerance
- **PostgreSQL**: Database
- **Docker**: Containerization
- **Prometheus & Grafana**: Monitoring

<!-- ## 🏛️ Design Patterns

- **Domain-Driven Design (DDD)**: Entities, value objects, domain events
- **CQRS**: Command/Query separation
- **Event-Driven Architecture**: Kafka-based messaging
- **Strategy Pattern**: Fraud rules
- **Composite Pattern**: Rule pipeline
- **Specification Pattern**: Conditional logic
- **Repository Pattern**: Data access abstraction
- **Outbox Pattern**: Reliable event publishing -->

## 📁 Project Structure

```
fraud-rule-engine/
├── src/
│   ├── FraudRuleEngine.Transactions.Api/
│   ├── FraudRuleEngine.Evaluations.Worker/
│   ├── FraudRuleEngine.Reporting.Api/
│   └── FraudRuleEngine.Shared/
├── infrastructure/
│   ├── kafka/
│   ├── postgres/
│   ├── grafana/
│   └── prometheus/
│ 
└── .env.example
└── .env (Needs to be created during initial setup)
└── docs/
└── docker-compose.yaml
```

## 🔧 Development

### Building the Solution

```bash
dotnet build FraudRuleEngine.sln
```

### Running Migrations

Migrations are automatically applied on startup. To create a new migration:

```bash
# Transactions API
cd src/FraudRuleEngine.Transactions.Api
dotnet ef migrations add MigrationName --context TransactionDbContext -o Data/Migrations

# Evaluations Worker
cd src/FraudRuleEngine.Evaluations.Worker
dotnet ef migrations add MigrationName --context FraudDbContext -o Data/Migrations

# Reporting API
cd src/FraudRuleEngine.Reporting.Api
dotnet ef migrations add MigrationName --context FraudReportingDbContext -o Data/Migrations
```

## 📊 Monitoring

- **Health Checks**: `/health` endpoint on each API
- **Prometheus Metrics**: Available at http://localhost:9090
- **Grafana Dashboards**: Pre-configured dashboards at http://localhost:3000

<!-- ## 🚧 Future Enhancements

- Event replay capability
- More sophisticated fraud rules
- Machine learning integration - might consider adding a fastapi + scikit learn model
- Real-time alerting
- API authentication/authorization 
- Schema registry for Kafka events -->