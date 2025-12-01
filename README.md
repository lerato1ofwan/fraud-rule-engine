# Fraud Rule Engine

A demo system for fraud detection using a custom rule engine implementation, built with .NET 8, following Domain-Driven Design (DDD), CQRS, and microservices with event-driven architecture patterns. 

Project Brief: \
Create a system that processes categorized transaction events and flags potential fraud. Apply a set of fraud rules per transaction based on different criteria and then store them in a data store. Allow the retrieval of this data via an API.

## 🏗️ Applications

The system consists of three microservices:

1. **Transactions API**: Ingress service for injesting, receiving and storing transactions
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

   Copy the .env.example and rename it to .env or run the below command:

   ```bash
   cp .env.example .env
   ```

3. **Configure your environment variables**:
   Open `.env` in your editor and update the following **mandatory** values:
   - `POSTGRES_PASSWORD`: Set a secure password for PostgreSQL (required)
   - `GRAFANA_ADMIN_PASSWORD`: Set a password for Grafana admin user (required)
   
   **Optional**: For development, you can use the default values provided in `.env.example`. For production, use strong, unique passwords.

   > **Note**: The `.env` file is already in `.gitignore` and will not be committed to version control.

### How To Run

1. Two options for running, a development simple version and the production stack.

   **Option 1: Simplified Setup**
   ```bash
   # 7 containers - Core functionality + basic observability
   docker-compose -f docker-compose.development.yml up -d
   ```
   This includes: PostgreSQL, Kafka, 3 application services, Prometheus, Grafana

   **Option 2: Full Production Stack**
   ```bash
   # 13 containers - Complete observability (metrics, logs, traces)
   docker-compose up -d
   ```
   This includes everything above plus: Loki, Promtail, Jaeger, Kafka UI

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
- **Jaegar:** http://localhost:16686/search
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
- Produces transactions.receieved messages to kafka to rule set evaluations

### Evaluations Worker
- Kafka consumer for transaction events
- Fraud rule engine with Strategy pattern
- Composite rule pipeline
- Specification pattern for conditional logic
- Request/RequestHandler Mediator pattern for data loading required by rules
- Multiple fraud rules to flag transactions
- Kafka producer for fraud assessment events
- Exponential backoffs on producer and consumer failures to store in dead letter queue

### Reporting API
- Kafka consumer for fraud assessment events
- Event projections for read models
- CQRS read side with optimized queries
- Analytics endpoints (summary, daily stats, top rules)
- Implementations and makes data evaluations to grafana dashboard (via prometheus metrics)

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

## 🧪 Testing (Unit Tests and Integration Testing)

Given the initial 8-10 days development constraint, my unit testing and integrations testing strategy focuses on only the most critical layers:

### Domain Logic Integrity (Unit Tests)
Focused on `FraudRuleEngine.Core` to ensure the set of fraud rules (e.g., `HighAmountRule`, `VelocityRule`, `ForeignCountryRule`) are mathematically and logically correct. Tests cover boundary conditions, edge cases (missing metadata, time window variations), and risk score aggregation logic. 

### End-to-End (Integration Tests)
Implemented in `FraudRuleEngine.Transactions.Api.Tests` using `WebApplicationFactory` and **Testcontainers** with real PostgreSQL databases using TestContainers. This validates the full DI, middleware pipeline, database interactions, and idempotency check.

**Why:**
- Prioritized domain rules and pipeline orchestration over controller unit tests (which test framework behavior rather than business logic)
- Chose integration tests over shallow unit tests to validate actual database persistence and event flow
- Used real PostgreSQL containers to catch EF Core mapping issues and migration problems that in-memory databases miss
- Future work should include increasing test code coverage to include Reporting.Api and Evaluations.Worker codebases

**Running Tests:**
```bash
# Run all tests
dotnet test

# Run only unit tests
dotnet test --filter "FullyQualifiedName~Core.Tests"

# Run only integration tests (requires Docker)
dotnet test --filter "FullyQualifiedName~Transactions.Api.Tests"
```

<!-- **Test Coverage:**
-  -->

### API Testing Examples

#### Create a Transaction

```bash
curl -X POST http://localhost:5000/api/transactions \
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

#### Get Fraud Summary

```bash
curl http://localhost:5001/api/fraud-reports/summary/{transactionId}
```

#### Get Daily Stats

```bash
curl http://localhost:5001/api/fraud-reports/stats/daily?date=2024-01-01
```

#### Get Top Rules

```bash
curl http://localhost:5001/api/fraud-reports/rules/top?top=10
```

### Generating Test Data with Postman Collection

To populate the system with test data for visualization in Grafana and Kafka UI, you can use the included Postman collection.

**Prerequisites**:
- Postman installed (or use the Postman CLI/Newman or Insomnia depending on preferrence)
- Services running (see [How To Run](#how-to-run) above)

**Using the Collection**:

1. **Import the collection**:
   - Open Postman
   - Click "Import" and select `transactions-import-collection.js` from the project root
   - The collection will appear as "Rule Engine Transaction Load Test"

2. **Run the collection**:
   - Open the collection
   - Click "Run" (or use the Runner)
   - Set iterations to **100** (or more for more data)
   - Click "Run Rule Engine Transaction Load Test"

3. **What it does**:
   - Generates random transaction data for each iteration
   - Random amounts (1-10,000), currencies, countries, IP addresses
   - 70% bias towards ZAR/RSA (South African context)
   - Unique external IDs in various formats
   - Sends POST requests to `http://localhost:5000/api/transactions`

4. **After running**:
   - Check Grafana dashboards for metrics and visualizations
   - View Kafka topics in Kafka UI (`http://localhost:8080`)
   - Check fraud reports via the Reporting API
   - Query fraud statistics and top rules

**Note**: The first run will show empty dashboards until transactions are processed. After running 100+ iterations, you should see:
- Transaction metrics in Grafana
- Events flowing through Kafka topics
- Fraud assessments being generated
- Reporting data populated

**Alternative: Using Newman (Postman CLI)**:
```bash
# Install Newman globally
npm install -g newman

# Run the collection
newman run transactions-import-collection.js -n 100
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

The project follows a clean architecture with clear separation of concerns:

```
fraud-rule-engine/
├── src/
│   ├── FraudRuleEngine.Transactions.Api/          
│   │   ├── Controllers/                            
│   │   ├── Data/                                  
│   │   │   ├── Migrations/                        
│   │   │   ├── Models/                             
│   │   │   ├── Repositories/                       
│   │   │   └── UnitOfWork/                         
│   │   ├── Domain/                                  
│   │   │   ├── DTOs/                               
│   │   │   ├── Events/                             
│   │   │   └── ValueObjects/                       
│   │   ├── Services/                              
│   │   │   ├── Behaviours/                         
│   │   │   ├── Commands/                           
│   │   │   ├── Queries/                            
│   │   │   └── Messaging/                          
│   │   ├── Dockerfile                              
│   │   └── Program.cs                              
│   │
│   ├── FraudRuleEngine.Evaluations.Worker/         
│   │   ├── Data/                                   
│   │   │   ├── Migrations/                         
│   │   │   ├── Models/                             
│   │   │   ├── Repositories/                       
│   │   │   └── Requests/                           
│   │   ├── Services/                               
│   │   ├── Workers/                                
│   │   ├── Dockerfile                              
│   │   └── Program.cs                              
│   │
│   ├── FraudRuleEngine.Reporting.Api/              
│   │   ├── Controllers/                           
│   │   ├── Data/                                   
│   │   │   ├── Migrations/                         
│   │   │   ├── Models/                             
│   │   │   └── Repositories/                       
│   │   ├── Domain/                                 
│   │   │   ├── DTOs/                               
│   │   │   └── ReadModels/                         
│   │   ├── Services/                               
│   │   │   ├── Metrics/                            
│   │   │   ├── Projections/                        
│   │   │   └── Queries/                            
│   │   ├── Workers/                                
│   │   ├── Metrics/                                
│   │   ├── Dockerfile                              
│   │   └── Program.cs                              
│   │
│   ├── FraudRuleEngine.Core/                       
│   │   └── Domain/                                 
│   │       ├── Rules/                              
│   │       ├── Specifications/                    
│   │       ├── ValueObjects/                      
│   │       ├── DataRequests/                      
│   │       ├── CompositeRulePipeline.cs           
│   │       └── IFraudRule.cs                      
│   │
│   └── FraudRuleEngine.Shared/                    
│       ├── Common/                                
│       │   └── Result.cs                          
│       ├── Contracts/                             
│       ├── Events/                                
│       ├── Messaging/                             
│       └── Metrics/                               
│
├── tests/
│   ├── FraudRuleEngine.Core.Tests/                 
│   │   ├── Domain/                                
│   │   └── Helpers/                            
│   │
│   └── FraudRuleEngine.Transactions.Api.Tests/    
│       ├── Abstractions/                           
│       └── Integration/                           
│
├── infrastructure/                                  
│   ├── grafana/                                    
│   │   ├── dashboards/                             
│   │   └── provisioning/                          
│   ├── kafka/                                    
│   ├── prometheus/                                
│   └── promtail/                                   
│
├── docs/                                           # Documentation
│   ├── architecture.md                            
│   ├── events.md                                   # Event schema documentation
│   ├── rules.md                                    # Fraud rules documentation
│   └── Rules and the Evaluation Service.md         # Rule engine design patterns
│
├── docker-compose.yaml                              
├── docker-compose.development.yml                  
├── FraudRuleEngine.sln                             
├── .env.example                                   
├── README-DEV.md                                   
├── README.md                                   
└── transactions-import-collection.js              # Postman/API collection for testing
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

- Increase test coverage 
- Event replay capability
- More sophisticated fraud rules
- Machine learning integration - might consider adding a fastapi + scikit learn model
- Real-time alerting
- API authentication/authorization 
- Schema registry for Kafka events -->