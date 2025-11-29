# Fraud Rule Engine - Simplified Development Setup

## Overview

This is a simplified version of the fraud rule engine focused on the core requirements:

1. **Process transaction events** - Transactions API injests or recieves and stores transactions
2. **Flags potential fraud** - Evaluations Worker processes transactions and applies a set of fraud rules per transaction 
3. **Store results** - Results stored in PostgreSQL
4. **Retrieve via API** - Reporting API provides endpoints to query transactions data both flagged and non-flagged

## Architecture

### Core Services

1. **PostgreSQL** - Data storage for all services
2. **Zookeeper + Kafka** - Event streaming for async processing
3. **Transactions API** - Receives transactions via REST API
4. **Evaluations Worker** - Applies fraud rules to transactions
5. **Reporting API** - Provides endpoints to retrieve fraud data
6. **Prometheus + Grafana** - Basic observability (metrics and dashboards)

### What I have simplified from the main docker-compose

- **Loki + Promtail** - Centralized logging (currently logs still available via `docker logs` but for production visualization is needed)
- **Jaeger** - Distributed tracing (can simply be added if needed but in prod it's a big requirements to help with debugging)
- **Kafka UI** - Kafka management UI (not required for core functionality, but great to visualize)

## Quick Start

### Using This Simplified Setup

```bash
# Use the development docker-compose
docker-compose -f docker-compose.development.yml up -d

# Or use the full setup (if you want all observability)
docker-compose up -d
```

### Verify Services

```bash
# Check all services are running
docker-compose -f docker-compose.development.yml ps

# View logs using container name
docker-compose -f docker-compose.development.yml logs -f transactions-api
```

## API Endpoints

### Transactions API
- `POST /api/transactions` - Create a transaction
- `GET /api/transactions/{id}` - Get transaction by ID

### Reporting API
- `GET /api/fraud-reports/summary/{transactionId}` - Get fraud summary for a transaction
- `GET /api/fraud-reports/stats/daily` - Get daily fraud statistics
- `GET /api/fraud-reports/rules/top` - Get top triggered rules used for flagging transactions

## Observability

- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3000 (admin/admin by default)