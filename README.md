# dotnet-outbox-inbox-pattern

## Infrastructure

The project uses **PostgreSQL** and **RabbitMQ**, managed via Docker Compose.

### Starting the containers

```bash
docker compose up -d
```

Both services have health checks configured. You can verify the status with:

```bash
docker compose ps
```

### Services

| Service       | Image                             | Ports                              |
| ------------- | --------------------------------- | ---------------------------------- |
| PostgreSQL 16 | `postgres:16-alpine`              | `5432`                             |
| RabbitMQ 3.13 | `rabbitmq:3.13-management-alpine` | `5672` (AMQP), `15672` (dashboard) |

### Connection strings

#### PostgreSQL

```plaintext
Host=localhost;Port=5432;Database=ordersdb;Username=postgres;Password=postgres
```

#### RabbitMQ

```plaintext
amqp://guest:guest@localhost:5672
```

#### RabbitMQ Management Dashboard

<http://localhost:15672>

- Username: `guest`
- Password: `guest`

## Running locally (without Docker)

**Pre-requisite:** PostgreSQL and RabbitMQ must be running. The easiest way is to start only the infrastructure containers:

```bash
docker compose up postgres rabbitmq -d
```

Open three separate terminals and run each service:

### Terminal 1 — Orders API

```bash
dotnet run --project src/Orders/Orders.API
```

### Terminal 2 — Inventory Consumer

```bash
dotnet run --project src/Inventory/Inventory.Consumer
```

### Terminal 3 — Notification Consumer

```bash
dotnet run --project src/Notification/Notification.Consumer
```

The Orders API will be available at `http://localhost:5000` (or the port shown in the terminal output after startup).

### Running Infra + Apps

```bash
docker-compose -f docker-compose.infra.yml up -d && docker-compose up -d
```

### Stop and Remove Infra + Apps

```bash
docker-compose -f docker-compose.infra.yml down -v && docker-compose down -v
```

## Stress test with K6

The file [tests/k6/outbox-stress-test.js](tests/k6/outbox-stress-test.js) runs a load test against the Orders API with the following stages:

| Stage     | Duration | Virtual Users |
| --------- | -------- | ------------- |
| Ramp-up   | 30s      | 0 → 10        |
| Sustained | 2m       | 50            |
| Ramp-down | 30s      | 50 → 0        |

**Thresholds:**

- 95th percentile response time < 500ms
- Error rate < 1%

### Pre-requisites

- [k6](https://k6.io/docs/get-started/installation/) installed
- Orders API running (see [Running locally](#running-locally-without-docker))

### Running the test

```bash
k6 run tests/k6/outbox-stress-test.js
```

To target a different API URL:

```bash
BASE_URL=http://localhost:5000 k6 run tests/k6/outbox-stress-test.js
```

---

## Testing the endpoint

### Using the `.http` file

Open `tests/Orders.API.http` in your IDE and send the request directly.

### Using cURL

```bash
curl -X POST http://localhost:5176/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "totalAmount": 150.00}'
```

Expected response (`201 Created`):

```json
{
  "id": "<generated-order-id>"
}
```
