# NotificationsAPI

Independent .NET 8 service for the Phase 2 simulated email notifications.

```powershell
dotnet build src/NotificationsAPI/NotificationsAPI.csproj
dotnet run --project src/NotificationsAPI/NotificationsAPI.csproj
```

`/health` reports process availability. RabbitMQ consumers write the Wave 2 welcome and approved-purchase messages to the console in pt-BR. No email provider is required.

## RabbitMQ configuration

The service declares the durable queues `notifications.user-created` and
`notifications.payment-processed` on the direct exchange `fcg.events`. It consumes
the routing keys `user.created` and `payment.processed` respectively.

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

- `UserCreatedEvent` produces one simulated welcome email in pt-BR.
- `PaymentProcessedEvent` with `Approved` produces one simulated purchase
  confirmation in pt-BR.
- `PaymentProcessedEvent` with `Rejected` produces no approval confirmation.

No real email provider is used. Event payloads and console output omit passwords,
tokens, and payment credentials.

## Container preparation

`Dockerfile` publishes this service from its own repository into a .NET 8 runtime image. The image runs as the built-in non-root user and listens on port 8080. From this repository, build with `docker build -t fcg-notifications-api .` when Docker is available. The sibling `Fase 2/Orchestration` repository provides the prepared Compose entry point.

The Docker Hub Personal workflow in `.github/workflows/docker-hub.yml` runs tests before publishing the public `viniciuscs84/fcg-notifications-api` image. The public GitHub repository is `viniciuscs84/fiap-postech-f2-fcg-notifications-api`, and its `DOCKERHUB_USERNAME` variable is configured. Add the `DOCKERHUB_TOKEN` secret after creating the public Docker Hub repository. Until then, the workflow validates the image build without publishing. See the orchestration README for image tags and pull commands.

## Tests

```powershell
dotnet test tests/NotificationsAPI.Tests/NotificationsAPI.Tests.csproj --configuration Release
```
