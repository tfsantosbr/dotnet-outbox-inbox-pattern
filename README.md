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

http://localhost:15672

- Username: `guest`
- Password: `guest`
