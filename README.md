# NotificationsAPI

Independent .NET 8 foundation for the Phase 2 simulated email service.

```powershell
dotnet build src/NotificationsAPI/NotificationsAPI.csproj
dotnet run --project src/NotificationsAPI/NotificationsAPI.csproj
```

`/health` reports process availability. The RabbitMQ consumers and pt-BR console messages for welcome and approved purchase notifications belong to Wave 2. No email provider is required.
