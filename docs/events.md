# Event Documentation

## Overview

I am using Kafka for event-driven communication between services. This keeps things loosely coupled - each service can work independently and scale on its own. Events flow through the system as transactions get processed and evaluated.

## Event Topics

All topic names are centralized in the `KafkaTopics` class to avoid magic strings scattered around the codebase.

### transaction.received

**Topic**: `transaction.received` (defined in `KafkaTopics.TransactionReceived`)  
**Published by**: Transactions API  
**Consumed by**: Evaluations Worker  
**Format**: JSON

This event gets published when a transaction is successfully saved to the database. It goes through the outbox pattern first - the event is written to the database in the same transaction as the transaction record, then a background service (`OutboxPublisher`) picks it up and publishes it to Kafka.

**Event Structure**:
```json
{
  "TransactionId": "123e4567-e89b-12d3-a456-426614174000",
  "AccountId": "123e4567-e89b-12d3-a456-426614174001",
  "Amount": 1000.00,
  "MerchantId": "123e4567-e89b-12d3-a456-426614174002",
  "Currency": "ZAR",
  "Timestamp": "2024-01-01T12:00:00Z",
  "Metadata": {
    "Country": "RSA",
    "IPAddress": "192.168.1.1",
    "DeviceId": "device-123"
  }
}
```

**What happens**:
- The Evaluations Worker picks this up and runs fraud rules against it
- We use `ExternalId` for idempotency - if the same external ID comes in twice, we return the existing transaction ID
- If publishing to Kafka fails, we retry with exponential backoff (2s, 4s, 8s delays)
- After all retries fail, the message goes to the DLQ

### fraud.assessed

**Topic**: `fraud.assessed` (defined in `KafkaTopics.FraudAssessed`)  
**Published by**: Evaluations Worker  
**Consumed by**: Reporting API  
**Format**: JSON

After the fraud rules run, we publish this event with the results. The Reporting API uses it to build read models for analytics.

**Event Structure**:
```json
{
  "FraudCheckId": "123e4567-e89b-12d3-a456-426614174003",
  "TransactionId": "123e4567-e89b-12d3-a456-426614174000",
  "IsFlagged": true,
  "OverallRiskScore": 0.75,
  "RuleResults": [
    {
      "RuleName": "HighAmountRule",
      "Triggered": true,
      "RiskScore": 0.7,
      "Reason": "Transaction amount 15000 exceeds threshold 10000"
    },
    {
      "RuleName": "VelocityRule",
      "Triggered": false,
      "RiskScore": 0.0,
      "Reason": "Rule did not trigger"
    },
    {
      "RuleName": "ForeignCountryRule",
      "Triggered": true,
      "RiskScore": 0.6,
      "Reason": "Transaction from foreign country: UK"
    }
  ]
}
```

**Risk Score Calculation**:
- `OverallRiskScore` is the average of all triggered rule risk scores
- `IsFlagged` is true if the overall risk score is 0.5 or higher
- Each rule can trigger independently and contribute to the final score

**What happens**:
- Reporting API consumes this and updates its read models
- We use upsert operations so it's idempotent - processing the same event twice won't cause duplicates

### dlq (Dead Letter Queue)

**Topic**: `dlq` (defined in `KafkaTopics.DeadLetterQueue`)  
**Published by**: Any service when message publishing fails after retries  
**Consumed by**: Monitoring/alerting (manual investigation)  
**Format**: JSON

When a message can't be published to Kafka after all retries (3 attempts with exponential backoff), we send it here. This prevents message loss and gives us a place to investigate what went wrong.

**Event Structure**:
```json
{
  "OriginalTopic": "transaction.received",
  "OriginalPayload": "{ ... original event JSON ... }",
  "FailureReason": "Connection timeout after 3 retries",
  "Timestamp": "2024-01-01T12:00:00Z",
  "ExceptionType": "ProduceException"
}
```

The message also includes Kafka headers with:
- `original-topic`: Where the message was supposed to go
- `failure-reason`: Why it failed
- `timestamp`: When it failed

**What happens**:
- These messages need manual investigation
- We log when messages hit the DLQ so we can alert on it
- If even DLQ publishing fails, we throw an exception - that's a critical failure

## How Events Flow Through the System

```
Client sends transaction
    ↓
Transactions API receives it
    ↓
Validates and saves to database
    ↓
Domain event added to outbox (same transaction)
    ↓
OutboxPublisher polls every 15 seconds
    ↓
Publishes to Kafka (transaction.received)
    ↓
Evaluations Worker consumes it
    ↓
Runs fraud rules
    ↓
Publishes result (fraud.assessed)
    ↓
Reporting Worker consumes it
    ↓
Updates read models for analytics
```

The outbox pattern ensures we don't lose events - even if Kafka is down when the transaction is saved, the event is safely stored in the database and will be published once Kafka is back up.

## Resilience and Error Handling

**Retry Strategy**:
- We use Polly for retries with exponential backoff
- 3 retry attempts with delays: 2 seconds, 4 seconds, 8 seconds
- Only transient errors are retried (network issues, timeouts)

**DLQ Fallback**:
- If all retries fail, the message goes to DLQ
- DLQ messages include the original payload plus failure metadata
- This prevents message loss and helps with debugging

**Consumer Error Handling**:
- If a consumer fails to process a message, we log it and continue
- For the Evaluations Worker, failed messages also go to DLQ
- The Reporting Worker logs errors but doesn't send to DLQ (it's the end of the chain)

## Idempotency

We handle duplicate events gracefully:

- **transaction.received**: Uses `ExternalId` - if we see the same external ID twice, we return the existing transaction ID
- **fraud.assessed**: Uses `TransactionId` - one assessment per transaction, upsert operations prevent duplicates
- **Reporting updates**: Upsert operations ensure idempotency

## Monitoring

**Metrics we track**:
- Event production rate (how many events per second)
- Event consumption rate
- Processing latency (how long from publish to consume)
- Error rates
- DLQ size (how many messages are stuck)

**What we alert on**:
- High error rates (>1% for 5 minutes)
- Consumer lag (messages piling up)
- Messages hitting DLQ
- Processing failures

## Event Schema

Right now we're using plain JSON. This gives us flexibility but means we need to be careful about breaking changes. In the future, we might add a schema registry (like Confluent Schema Registry) to:
- Validate events match expected schemas
- Handle schema evolution better
- Check compatibility between versions

For now, we keep backward compatibility by:
- Adding new fields as optional
- Not removing fields (mark as deprecated instead)
- Using the centralized `KafkaTopics` and `EventTypes` classes

## Testing Events

**Unit tests**: We test event serialization/deserialization and handler logic

**Integration tests**: We test the full flow - publish event, consume it, verify the result

**Load tests**: We've tested high-volume scenarios to make sure the system can handle the expected throughput

## Best Practices To Follow

1. **Keep events small** - Under 1MB, ideally much smaller
2. **Design for idempotency** - Always assume events might be processed twice
3. **Retry transient failures** - Network hiccups shouldn't cause message loss
4. **Monitor everything** - Track production, consumption, errors, latency
5. **Handle DLQ messages** - Don't let them pile up, investigate and fix issues
6. **Use centralized constants** - `KafkaTopics` and `EventTypes` classes prevent typos
