# NotificationsAPI

`/health/live` reports process liveness; `/health/ready` requires both notification consumers to be subscribed to RabbitMQ. A broker disconnect changes readiness to HTTP 503 while liveness remains available. `/health` remains a liveness alias. The consumer failure counter is written to `/tmp/fcg-metrics.json` inside the container, without a public metrics endpoint.

Independent .NET 8 service for the Phase 2 simulated email notifications.

```powershell
dotnet build src/NotificationsAPI/NotificationsAPI.csproj
dotnet run --project src/NotificationsAPI/NotificationsAPI.csproj
```

`/health` reports process availability. RabbitMQ consumers write the Wave 2 welcome and approved-purchase messages to the console in pt-BR. No email provider is required.

## RabbitMQ configuration

The service declares durable queues `notifications.user-created` and
`notifications.payment-processed` on the direct exchange `fcg.events`. It consumes
the routing keys `user.created` and `payment.processed` respectively. Invalid
events and events that fail after three processing attempts are published with
broker confirmation to the corresponding `.dead` queue before the original
delivery is acknowledged. A failed dead-letter publication requeues the original.

Configuration uses the `RabbitMq` section. Supply the local broker password through
an environment variable so credentials are not stored in the repository:

```powershell
$env:RabbitMq__Password = "<local-rabbitmq-password>"
dotnet run --project src/NotificationsAPI/NotificationsAPI.csproj
```

`HostName`, `Port`, `UserName`, and `Exchange` can be overridden with the same
double-underscore environment-variable convention. The service retries the initial
broker connection while continuing to expose its health endpoint.

## Observable notifications

- Version 2 `UserCreatedEvent` carries an `EventId`; version 2
  `PaymentProcessedEvent` carries `EventId` and `OrderId`. Older queued
  messages must be drained before upgrading the producer and consumer images.
- `UserCreatedEvent` produces one simulated welcome email in pt-BR.
- `PaymentProcessedEvent` with `Approved` produces one simulated purchase
  confirmation in pt-BR.
- `PaymentProcessedEvent` with `Rejected` produces no approval confirmation.

No real email provider is used. Event payloads and console output omit passwords,
tokens, and payment credentials. Console delivery is at least once: a restart
after output but before RabbitMQ acknowledgement can print the same simulated
message again. Replayed events retain their original `EventId`.

## Container preparation

`Dockerfile` publishes this service from its own repository into a .NET 8 runtime image. The image runs as the built-in non-root user and listens on port 8080. From this repository, build with `docker build -t fcg-notifications-api .` when Docker is available. The sibling `Fase 2/Orchestration` repository provides the prepared Compose entry point.

The Docker Hub Personal workflow in `.github/workflows/docker-hub.yml` runs tests before publishing the public `viniciuscs84/fcg-notifications-api` image. The public GitHub repository is `viniciuscs84/fiap-postech-f2-fcg-notifications-api`. Its `DOCKERHUB_USERNAME` variable and encrypted `DOCKERHUB_TOKEN` secret are configured. The image is published with `main` and full-commit `sha-...` tags. See the orchestration README for pull commands and token rotation.

## Local Kubernetes manifest

`k8s/all.yaml` owns this service's Deployment and ClusterIP Service in namespace `fcg-phase2`. The shared ConfigMap supplies the RabbitMQ Service name and the shared Secret supplies broker credentials. Kind setup and apply order are documented in the sibling `Orchestration/README.md`. The Deployment runs as non-root UID 1654.

## Tests

```powershell
dotnet test tests/NotificationsAPI.Tests/NotificationsAPI.Tests.csproj --configuration Release
```
